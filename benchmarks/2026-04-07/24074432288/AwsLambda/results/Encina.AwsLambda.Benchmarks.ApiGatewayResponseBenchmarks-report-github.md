```

BenchmarkDotNet v0.15.8, Linux Ubuntu 24.04.4 LTS (Noble Numbat)
AMD EPYC 7763 3.14GHz, 1 CPU, 4 logical and 2 physical cores
.NET SDK 10.0.201
  [Host]     : .NET 10.0.5 (10.0.5, 10.0.526.15411), X64 RyuJIT x86-64-v3
  DefaultJob : .NET 10.0.5 (10.0.5, 10.0.526.15411), X64 RyuJIT x86-64-v3
  MediumRun  : .NET 10.0.5 (10.0.5, 10.0.526.15411), X64 RyuJIT x86-64-v3


```
| Method                       | Job        | IterationCount | LaunchCount | WarmupCount | Mean        | Error     | StdDev    | Median      | Ratio | RatioSD | Gen0   | Allocated | Alloc Ratio |
|----------------------------- |----------- |--------------- |------------ |------------ |------------:|----------:|----------:|------------:|------:|--------:|-------:|----------:|------------:|
| ToApiGatewayResponse_Success | DefaultJob | Default        | Default     | Default     |   808.63 ns |  2.751 ns |  2.297 ns |   807.79 ns |  1.00 |    0.00 | 0.0296 |     496 B |        1.00 |
| ToApiGatewayResponse_Error   | DefaultJob | Default        | Default     | Default     | 1,563.52 ns |  2.672 ns |  2.369 ns | 1,563.50 ns |  1.93 |    0.01 | 0.0725 |    1232 B |        2.48 |
| ToCreatedResponse_Success    | DefaultJob | Default        | Default     | Default     |   892.58 ns |  1.346 ns |  1.051 ns |   892.50 ns |  1.10 |    0.00 | 0.0286 |     480 B |        0.97 |
| ToNoContentResponse_Success  | DefaultJob | Default        | Default     | Default     |    15.37 ns |  0.129 ns |  0.120 ns |    15.34 ns |  0.02 |    0.00 | 0.0029 |      48 B |        0.10 |
| ToHttpApiResponse_Success    | DefaultJob | Default        | Default     | Default     |   815.64 ns |  1.582 ns |  1.402 ns |   815.65 ns |  1.01 |    0.00 | 0.0296 |     496 B |        1.00 |
| ToHttpApiResponse_Error      | DefaultJob | Default        | Default     | Default     | 1,506.14 ns |  1.441 ns |  1.203 ns | 1,506.03 ns |  1.86 |    0.01 | 0.0725 |    1232 B |        2.48 |
|                              |            |                |             |             |             |           |           |             |       |         |        |           |             |
| ToApiGatewayResponse_Success | MediumRun  | 15             | 2           | 10          |   789.89 ns |  4.743 ns |  7.099 ns |   789.77 ns |  1.00 |    0.01 | 0.0296 |     496 B |        1.00 |
| ToApiGatewayResponse_Error   | MediumRun  | 15             | 2           | 10          | 1,566.19 ns |  8.371 ns | 12.006 ns | 1,567.22 ns |  1.98 |    0.02 | 0.0725 |    1232 B |        2.48 |
| ToCreatedResponse_Success    | MediumRun  | 15             | 2           | 10          |   908.32 ns |  4.531 ns |  6.498 ns |   908.25 ns |  1.15 |    0.01 | 0.0286 |     480 B |        0.97 |
| ToNoContentResponse_Success  | MediumRun  | 15             | 2           | 10          |    14.79 ns |  0.090 ns |  0.129 ns |    14.79 ns |  0.02 |    0.00 | 0.0029 |      48 B |        0.10 |
| ToHttpApiResponse_Success    | MediumRun  | 15             | 2           | 10          |   792.21 ns |  2.591 ns |  3.798 ns |   792.19 ns |  1.00 |    0.01 | 0.0296 |     496 B |        1.00 |
| ToHttpApiResponse_Error      | MediumRun  | 15             | 2           | 10          | 1,569.56 ns | 18.279 ns | 25.625 ns | 1,585.04 ns |  1.99 |    0.04 | 0.0725 |    1232 B |        2.48 |
