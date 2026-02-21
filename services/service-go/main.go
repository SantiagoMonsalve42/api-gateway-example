package main

import (
	"encoding/json"
	"log"
	"net/http"
	"time"
)

type Order struct {
	ID     string  `json:"id"`
	Amount float64 `json:"amount"`
	Status string  `json:"status"`
}

type Response struct {
	Orders  []Order           `json:"orders"`
}

type ErrorResponse struct {
	Error string `json:"error"`
}

func ordersHandler(w http.ResponseWriter, r *http.Request) {
	// Si el minuto actual es par, retornar error 500
	currentMinute := time.Now().Minute()
	if currentMinute%2 == 0 {
		log.Printf("Minute %d is even, returning 500 error", currentMinute)
		w.Header().Set("Content-Type", "application/json")
		w.WriteHeader(http.StatusInternalServerError)
		json.NewEncoder(w).Encode(ErrorResponse{
			Error: "Service temporarily unavailable (testing circuit breaker)",
		})
		return
	}

	orders := []Order{
		{ID: "ORD-001", Amount: 120.50, Status: "CREATED"},
		{ID: "ORD-002", Amount: 89.99, Status: "PAID"},
		{ID: "ORD-003", Amount: 45.00, Status: "SHIPPED"},
	}

	response := Response{
		Orders:  orders,
	}

	w.Header().Set("Content-Type", "application/json")
	json.NewEncoder(w).Encode(response)
}

func main() {
	http.HandleFunc("/orders", ordersHandler)

	log.Println("Service Go running on port 8080")
	log.Fatal(http.ListenAndServe(":8080", nil))
}