```

BenchmarkDotNet v0.15.8, Linux Ubuntu 24.04.5 LTS (Noble Numbat)
AMD EPYC 7763 2.45GHz, 1 CPU, 4 logical and 2 physical cores
.NET SDK 10.0.401
  [Host]     : .NET 10.0.12 (10.0.12, 10.0.1226.42308), X64 RyuJIT x86-64-v3
  DefaultJob : .NET 10.0.12 (10.0.12, 10.0.1226.42308), X64 RyuJIT x86-64-v3
  MediumRun  : .NET 10.0.12 (10.0.12, 10.0.1226.42308), X64 RyuJIT x86-64-v3


```
| Method                       | Job        | IterationCount | LaunchCount | WarmupCount | Mean        | Error     | StdDev    | Median      | Ratio | RatioSD | Gen0   | Allocated | Alloc Ratio |
|----------------------------- |----------- |--------------- |------------ |------------ |------------:|----------:|----------:|------------:|------:|--------:|-------:|----------:|------------:|
| ToCreatedResponse_Success    | DefaultJob | Default        | Default     | Default     |   889.44 ns |  3.735 ns |  3.494 ns |   888.76 ns |  1.11 |    0.00 | 0.0286 |     480 B |        0.97 |
| ToNoContentResponse_Success  | DefaultJob | Default        | Default     | Default     |    15.36 ns |  0.138 ns |  0.129 ns |    15.38 ns |  0.02 |    0.00 | 0.0029 |      48 B |        0.10 |
| ToHttpApiResponse_Success    | DefaultJob | Default        | Default     | Default     |   818.92 ns |  1.845 ns |  1.541 ns |   818.56 ns |  1.02 |    0.00 | 0.0296 |     496 B |        1.00 |
| ToHttpApiResponse_Error      | DefaultJob | Default        | Default     | Default     | 1,526.35 ns |  5.080 ns |  4.503 ns | 1,525.16 ns |  1.91 |    0.01 | 0.0725 |    1232 B |        2.48 |
| ToApiGatewayResponse_Error   | DefaultJob | Default        | Default     | Default     | 1,515.86 ns |  1.523 ns |  1.350 ns | 1,515.63 ns |  1.89 |    0.00 | 0.0725 |    1232 B |        2.48 |
| ToApiGatewayResponse_Success | DefaultJob | Default        | Default     | Default     |   800.29 ns |  1.466 ns |  1.371 ns |   799.99 ns |  1.00 |    0.00 | 0.0296 |     496 B |        1.00 |
|                              |            |                |             |             |             |           |           |             |       |         |        |           |             |
| ToCreatedResponse_Success    | MediumRun  | 15             | 2           | 10          |   889.43 ns |  4.835 ns |  6.934 ns |   889.37 ns |  1.11 |    0.01 | 0.0286 |     480 B |        0.97 |
| ToNoContentResponse_Success  | MediumRun  | 15             | 2           | 10          |    15.13 ns |  0.135 ns |  0.194 ns |    15.11 ns |  0.02 |    0.00 | 0.0029 |      48 B |        0.10 |
| ToHttpApiResponse_Success    | MediumRun  | 15             | 2           | 10          |   819.66 ns | 12.597 ns | 18.465 ns |   835.02 ns |  1.02 |    0.02 | 0.0296 |     496 B |        1.00 |
| ToHttpApiResponse_Error      | MediumRun  | 15             | 2           | 10          | 1,514.22 ns |  9.398 ns | 13.775 ns | 1,508.31 ns |  1.89 |    0.02 | 0.0725 |    1232 B |        2.48 |
| ToApiGatewayResponse_Error   | MediumRun  | 15             | 2           | 10          | 1,527.45 ns | 13.279 ns | 19.464 ns | 1,517.43 ns |  1.90 |    0.02 | 0.0725 |    1232 B |        2.48 |
| ToApiGatewayResponse_Success | MediumRun  | 15             | 2           | 10          |   803.15 ns |  1.289 ns |  1.849 ns |   803.15 ns |  1.00 |    0.00 | 0.0296 |     496 B |        1.00 |
