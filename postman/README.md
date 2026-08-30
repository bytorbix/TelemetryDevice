# Postman Collection

Import `Telemetry Device.postman_collection.json` into Postman (Import -> Files) to hit this service's API locally.

## Requests

- **Register Tail Number** — register a UAV tail number with the device.
- **Start Listener** — starts the pcap listener. `Ip` must be the current docker-network IP of the container sending UDP traffic (e.g. `telemetry-sim`); it does not resolve container DNS names, so re-check the IP with `docker inspect` if that container has been recreated.
- **Get Active Tail Numbers** — lists tail numbers currently registered.
- **Stop Listener** — stops the pcap listener.
- **Remove Tail Number** — unregisters a tail number.

## Typical order

1. Register Tail Number
2. Start Listener
3. (send telemetry from the simulator)
4. Stop Listener when done
