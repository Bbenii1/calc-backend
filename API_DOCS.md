# Scientific Calculator Backend API Documentation

This backend provides mathematical calculation and number system conversion services for the Scientific Calculator project.

## Base URL
The default base URL for the API is `http://localhost:8081`.

## Endpoints

### 1. Calculate Expression
Evaluates a mathematical expression.

- **URL**: `/api/Calculator/calculate`
- **Method**: `GET`
- **Query Parameters**:
    - `expression` (string, required): The mathematical expression to evaluate (e.g., `2+2`, `sin(pi/2)`, `5!`).
- **Success Response**:
    - **Code**: 200 OK
    - **Content**: `{ "result": <number> }`
- **Error Response**:
    - **Code**: 400 Bad Request
    - **Content**: `{ "error": "<error message>" }`

#### Example
`GET http://localhost:8081/api/Calculator/calculate?expression=2%2B(3*4)`
Response: `{ "result": 14 }`

---

### 2. Convert Number System
Converts a number from one base to another.

- **URL**: `/api/Calculator/convert`
- **Method**: `GET`
- **Query Parameters**:
    - `value` (string, required): The value to convert.
    - `fromBase` (integer, required): The source base (supported: 2, 8, 10, 16).
    - `toBase` (integer, required): The target base (supported: 2, 8, 10, 16).
- **Success Response**:
    - **Code**: 200 OK
    - **Content**: `{ "result": "<string>" }`
- **Error Response**:
    - **Code**: 400 Bad Request
    - **Content**: `{ "error": "<error message>" }`

#### Example
`GET http://localhost:8081/api/Calculator/convert?value=1010&fromBase=2&toBase=10`
Response: `{ "result": "10" }`

---

## Supported Operations
The backend supports the following operations in the `calculate` endpoint:

- **Basic Arithmetic**: `+`, `-`, `*`, `/`, `^`
- **Scientific Functions**: `sin`, `cos`, `tan`, `log` (log10), `ln` (log e), `sqrt`
- **Advanced Operations**: `!` (factorial), `%` (percent)
- **Constants**: `pi`, `e`
- **Misc**: `rand` (returns a random number between 0 and 1)

## Base Conversion
Supported bases:
- **2**: Binary
- **8**: Octal
- **10**: Decimal
- **16**: Hexadecimal
