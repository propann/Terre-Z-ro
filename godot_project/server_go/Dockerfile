FROM golang:1.24-bookworm AS build

WORKDIR /src

RUN apt-get update \
    && apt-get install -y --no-install-recommends build-essential \
    && rm -rf /var/lib/apt/lists/*

COPY go.mod ./
RUN go mod download

COPY . .

RUN CGO_ENABLED=1 go build -trimpath -o /out/terre-zero-backend .

FROM debian:bookworm-slim

RUN apt-get update \
    && apt-get install -y --no-install-recommends ca-certificates \
    && rm -rf /var/lib/apt/lists/*

COPY --from=build /out/terre-zero-backend /usr/local/bin/terre-zero-backend

EXPOSE 8080

ENTRYPOINT ["/usr/local/bin/terre-zero-backend"]