const express = require('express');
const app = express();

const PORT = 3000;

app.get('/users', (req, res) => {
  res.json({
    users: [
      { id: 1, name: "Alice" , address: "123 Main St"},
      { id: 2, name: "Bob" , address: "456 Elm St"},
      { id: 3, name: "Charlie" , address: "789 Oak St"}
    ]   
  });
});

app.listen(PORT, () => {
  console.log(`Service Node running on port ${PORT}`);
});