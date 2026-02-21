package com.demo.sales;

import org.springframework.boot.SpringApplication;
import org.springframework.boot.autoconfigure.SpringBootApplication;
import org.springframework.web.bind.annotation.GetMapping;
import org.springframework.web.bind.annotation.RequestHeader;
import org.springframework.web.bind.annotation.RestController;

import java.util.HashMap;
import java.util.List;
import java.util.Map;

@SpringBootApplication
public class Application {

    public static void main(String[] args) {
        SpringApplication.run(Application.class, args);
    }

    @RestController
    class SalesController {

        @GetMapping("/sales")
        public Map<String, Object> getSales() {
            return Map.of(
                    "sales", List.of(
                            Map.of("id", "SALE-001", "total", 499.99, "currency", "USD", "status", "COMPLETED"),
                            Map.of("id", "SALE-002", "total", 129.50, "currency", "USD", "status", "PENDING"),
                            Map.of("id", "SALE-003", "total", 89.00, "currency", "USD", "status", "CANCELLED")
                    )
            );
        }
    }
}