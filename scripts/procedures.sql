CREATE OR ALTER PROCEDURE SP_ADD_ORDER
(P_USER_ID BIGINT, P_PRODUCT_ID BIGINT, P_QUANTITY SMALLINT)
AS
BEGIN
    INSERT INTO orders(user_id, product_id, quantity) 
    VALUES (:p_user_id, :p_product_id, :p_quantity);
END
;

CREATE OR ALTER PROCEDURE SP_ADD_PRODUCT
(P_NAME VARCHAR(100), P_PRICE NUMERIC(10,2))
AS
BEGIN
    INSERT INTO products(name, price) VALUES (:p_name, :p_price);
END
;

CREATE OR ALTER PROCEDURE SP_GET_USER
(P_ID BIGINT)
RETURNS (R_NAME VARCHAR(100), R_EMAIL VARCHAR(255), R_AGE SMALLINT)
AS
BEGIN
    FOR
        SELECT name, email, age
        FROM users
        WHERE id = :p_id
        INTO :r_name, :r_email, :r_age
    DO
        SUSPEND;
END
;

