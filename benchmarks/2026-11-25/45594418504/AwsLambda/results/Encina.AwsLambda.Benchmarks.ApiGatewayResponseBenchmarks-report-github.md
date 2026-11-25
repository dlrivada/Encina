```

BenchmarkDotNet v0.15.8, Linux Ubuntu 26.04.1 LTS (Resolute Raccoon)
Intel Xeon 6975P-C 2.70GHz, 1 CPU, 4 logical and 2 physical cores
.NET SDK 10.0.404
  [Host]     : .NET 10.0.15 (10.0.15, 10.0.1526.53008), X64 RyuJIT x86-64-v4
  DefaultJob : .NET 10.0.15 (10.0.15, 10.0.1526.53008), X64 RyuJIT x86-64-v4
  ShortRun   : .NET 10.0.15 (10.0.15, 10.0.1526.53008), X64 RyuJIT x86-64-v4


```
| Method                       | Job        | IterationCount | LaunchCount | WarmupCount | Mean        | Error     | StdDev   | Ratio | Gen0   | Allocated | Alloc Ratio |
|----------------------------- |----------- |--------------- |------------ |------------ |------------:|----------:|---------:|------:|-------:|----------:|------------:|
| ToCreatedResponse_Success    | DefaultJob | Default        | Default     | Default     |   714.07 ns |  1.900 ns | 1.777 ns |  1.38 | 0.0057 |     480 B |        0.97 |
| ToNoContentResponse_Success  | DefaultJob | Default        | Default     | Default     |    15.30 ns |  0.279 ns | 0.248 ns |  0.03 | 0.0006 |      48 B |        0.10 |
| ToHttpApiResponse_Success    | DefaultJob | Default        | Default     | Default     |   529.93 ns |  1.455 ns | 1.361 ns |  1.02 | 0.0057 |     496 B |        1.00 |
| ToHttpApiResponse_Error      | DefaultJob | Default        | Default     | Default     | 1,032.21 ns |  3.826 ns | 3.392 ns |  1.99 | 0.0134 |    1176 B |        2.37 |
| ToApiGatewayResponse_Error   | DefaultJob | Default        | Default     | Default     | 1,061.15 ns |  3.698 ns | 3.279 ns |  2.05 | 0.0134 |    1176 B |        2.37 |
| ToApiGatewayResponse_Success | DefaultJob | Default        | Default     | Default     |   517.67 ns |  0.357 ns | 0.298 ns |  1.00 | 0.0057 |     496 B |        1.00 |
|                              |            |                |             |             |             |           |          |       |        |           |             |
| ToCreatedResponse_Success    | ShortRun   | 3              | 1           | 3           |   715.82 ns | 22.962 ns | 1.259 ns |  1.35 | 0.0057 |     480 B |        0.97 |
| ToNoContentResponse_Success  | ShortRun   | 3              | 1           | 3           |    12.63 ns |  5.130 ns | 0.281 ns |  0.02 | 0.0006 |      48 B |        0.10 |
| ToHttpApiResponse_Success    | ShortRun   | 3              | 1           | 3           |   515.69 ns | 19.300 ns | 1.058 ns |  0.97 | 0.0057 |     496 B |        1.00 |
| ToHttpApiResponse_Error      | ShortRun   | 3              | 1           | 3           | 1,037.26 ns |  8.055 ns | 0.441 ns |  1.95 | 0.0134 |    1176 B |        2.37 |
| ToApiGatewayResponse_Error   | ShortRun   | 3              | 1           | 3           | 1,002.13 ns | 95.562 ns | 5.238 ns |  1.89 | 0.0134 |    1176 B |        2.37 |
| ToApiGatewayResponse_Success | ShortRun   | 3              | 1           | 3           |   531.62 ns |  3.283 ns | 0.180 ns |  1.00 | 0.0057 |     496 B |        1.00 |
