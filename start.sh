#!/bin/bash

# PostgreSQL Docker Compose Management Script

set -e

# Colors for output
RED='\033[0;31m'
GREEN='\033[0;32m'
YELLOW='\033[1;33m'
BLUE='\033[0;34m'
NC='\033[0m' # No Color

# Function to print colored output
print_status() {
    echo -e "${BLUE}[INFO]${NC} $1"
}

print_success() {
    echo -e "${GREEN}[SUCCESS]${NC} $1"
}

print_warning() {
    echo -e "${YELLOW}[WARNING]${NC} $1"
}

print_error() {
    echo -e "${RED}[ERROR]${NC} $1"
}

# Function to check if Docker is running
check_docker() {
    if ! docker info > /dev/null 2>&1; then
        print_error "Docker is not running. Please start OrbStack or Docker Desktop."
        exit 1
    fi
    print_success "Docker is running"
}

# Function to check if ports are available
check_ports() {
    local postgres_port=${POSTGRES_PORT:-5432}
    local pgadmin_port=${PGADMIN_PORT:-8080}
    
    if lsof -Pi :$postgres_port -sTCP:LISTEN -t >/dev/null 2>&1; then
        print_warning "Port $postgres_port is already in use"
        read -p "Do you want to continue anyway? (y/N): " -n 1 -r
        echo
        if [[ ! $REPLY =~ ^[Yy]$ ]]; then
            exit 1
        fi
    fi
    
    if lsof -Pi :$pgadmin_port -sTCP:LISTEN -t >/dev/null 2>&1; then
        print_warning "Port $pgadmin_port is already in use"
        read -p "Do you want to continue anyway? (y/N): " -n 1 -r
        echo
        if [[ ! $REPLY =~ ^[Yy]$ ]]; then
            exit 1
        fi
    fi
}

# Function to start services
start_services() {
    print_status "Starting PostgreSQL services..."
    docker-compose up -d
    
    print_status "Waiting for PostgreSQL to be ready..."
    local max_attempts=30
    local attempt=1
    
    while [ $attempt -le $max_attempts ]; do
        if docker-compose exec -T postgres pg_isready -U postgres > /dev/null 2>&1; then
            print_success "PostgreSQL is ready!"
            break
        fi
        
        print_status "Waiting for PostgreSQL... (attempt $attempt/$max_attempts)"
        sleep 2
        attempt=$((attempt + 1))
    done
    
    if [ $attempt -gt $max_attempts ]; then
        print_error "PostgreSQL failed to start within the expected time"
        docker-compose logs postgres
        exit 1
    fi
}

# Function to show status
show_status() {
    print_status "Service Status:"
    docker-compose ps
    
    echo
    print_status "Connection Information:"
    echo "PostgreSQL: localhost:${POSTGRES_PORT:-5432}"
    echo "Database: ${POSTGRES_DB:-myapp}"
    echo "Username: ${POSTGRES_USER:-postgres}"
    echo "Password: ${POSTGRES_PASSWORD:-postgres_password}"
    echo
    echo "pgAdmin: http://localhost:${PGADMIN_PORT:-8080}"
    echo "Email: ${PGADMIN_DEFAULT_EMAIL:-admin@example.com}"
    echo "Password: ${PGADMIN_DEFAULT_PASSWORD:-admin_password}"
}

# Function to show logs
show_logs() {
    print_status "Showing PostgreSQL logs (Ctrl+C to exit):"
    docker-compose logs -f postgres
}

# Function to stop services
stop_services() {
    print_status "Stopping PostgreSQL services..."
    docker-compose down
    print_success "Services stopped"
}

# Function to restart services
restart_services() {
    print_status "Restarting PostgreSQL services..."
    docker-compose restart
    print_success "Services restarted"
}

# Function to show help
show_help() {
    echo "PostgreSQL Docker Compose Management Script"
    echo
    echo "Usage: $0 [COMMAND]"
    echo
    echo "Commands:"
    echo "  start     Start PostgreSQL services (default)"
    echo "  stop      Stop PostgreSQL services"
    echo "  restart   Restart PostgreSQL services"
    echo "  status    Show service status and connection info"
    echo "  logs      Show PostgreSQL logs"
    echo "  help      Show this help message"
    echo
    echo "Examples:"
    echo "  $0 start"
    echo "  $0 status"
    echo "  $0 logs"
}

# Main script logic
main() {
    local command=${1:-start}
    
    case $command in
        start)
            check_docker
            check_ports
            start_services
            show_status
            ;;
        stop)
            stop_services
            ;;
        restart)
            restart_services
            show_status
            ;;
        status)
            show_status
            ;;
        logs)
            show_logs
            ;;
        help|--help|-h)
            show_help
            ;;
        *)
            print_error "Unknown command: $command"
            show_help
            exit 1
            ;;
    esac
}

# Run main function with all arguments
main "$@" 