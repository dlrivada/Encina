```

BenchmarkDotNet v0.15.8, Linux Ubuntu 24.04.5 LTS (Noble Numbat)
AMD EPYC 9V74 2.60GHz, 1 CPU, 4 logical and 2 physical cores
.NET SDK 10.0.401
  [Host]     : .NET 10.0.12 (10.0.12, 10.0.1226.42308), X64 RyuJIT x86-64-v4
  DefaultJob : .NET 10.0.12 (10.0.12, 10.0.1226.42308), X64 RyuJIT x86-64-v4
  MediumRun  : .NET 10.0.12 (10.0.12, 10.0.1226.42308), X64 RyuJIT x86-64-v4


```
| Method                       | Job        | IterationCount | LaunchCount | WarmupCount | Mean        | Error     | StdDev    | Ratio | RatioSD | Gen0   | Allocated | Alloc Ratio |
|----------------------------- |----------- |--------------- |------------ |------------ |------------:|----------:|----------:|------:|--------:|-------:|----------:|------------:|
| ToCreatedResponse_Success    | DefaultJob | Default        | Default     | Default     |   592.58 ns |  5.007 ns |  4.684 ns |  1.20 |    0.01 | 0.0286 |     480 B |        0.97 |
| ToNoContentResponse_Success  | DefaultJob | Default        | Default     | Default     |    10.47 ns |  0.110 ns |  0.103 ns |  0.02 |    0.00 | 0.0029 |      48 B |        0.10 |
| ToHttpApiResponse_Success    | DefaultJob | Default        | Default     | Default     |   551.62 ns |  6.022 ns |  5.633 ns |  1.12 |    0.02 | 0.0296 |     496 B |        1.00 |
| ToHttpApiResponse_Error      | DefaultJob | Default        | Default     | Default     | 1,117.01 ns | 21.380 ns | 21.956 ns |  2.26 |    0.05 | 0.0725 |    1232 B |        2.48 |
| ToApiGatewayResponse_Error   | DefaultJob | Default        | Default     | Default     | 1,134.03 ns | 19.015 ns | 17.787 ns |  2.30 |    0.04 | 0.0725 |    1232 B |        2.48 |
| ToApiGatewayResponse_Success | DefaultJob | Default        | Default     | Default     |   494.16 ns |  5.293 ns |  4.951 ns |  1.00 |    0.01 | 0.0296 |     496 B |        1.00 |
|                              |            |                |             |             |             |           |           |       |         |        |           |             |
| ToCreatedResponse_Success    | MediumRun  | 15             | 2           | 10          |   552.73 ns |  4.737 ns |  7.089 ns |  1.12 |    0.02 | 0.0286 |     480 B |        0.97 |
| ToNoContentResponse_Success  | MediumRun  | 15             | 2           | 10          |    10.98 ns |  0.174 ns |  0.255 ns |  0.02 |    0.00 | 0.0029 |      48 B |        0.10 |
| ToHttpApiResponse_Success    | MediumRun  | 15             | 2           | 10          |   502.12 ns |  6.342 ns |  9.295 ns |  1.02 |    0.02 | 0.0296 |     496 B |        1.00 |
| ToHttpApiResponse_Error      | MediumRun  | 15             | 2           | 10          | 1,123.57 ns |  7.881 ns | 11.552 ns |  2.28 |    0.03 | 0.0725 |    1232 B |        2.48 |
| ToApiGatewayResponse_Error   | MediumRun  | 15             | 2           | 10          | 1,138.75 ns |  9.850 ns | 14.743 ns |  2.31 |    0.04 | 0.0725 |    1232 B |        2.48 |
| ToApiGatewayResponse_Success | MediumRun  | 15             | 2           | 10          |   493.29 ns |  3.553 ns |  5.318 ns |  1.00 |    0.02 | 0.0296 |     496 B |        1.00 |
