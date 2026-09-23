```

BenchmarkDotNet v0.15.8, Linux Ubuntu 24.04.5 LTS (Noble Numbat)
AMD EPYC 9V45 2.60GHz, 1 CPU, 4 logical and 2 physical cores
.NET SDK 10.0.401
  [Host]     : .NET 10.0.12 (10.0.12, 10.0.1226.42308), X64 RyuJIT x86-64-v4
  DefaultJob : .NET 10.0.12 (10.0.12, 10.0.1226.42308), X64 RyuJIT x86-64-v4
  ShortRun   : .NET 10.0.12 (10.0.12, 10.0.1226.42308), X64 RyuJIT x86-64-v4


```
| Method                       | Job        | IterationCount | LaunchCount | WarmupCount | Mean       | Error       | StdDev     | Ratio | RatioSD | Gen0   | Allocated | Alloc Ratio |
|----------------------------- |----------- |--------------- |------------ |------------ |-----------:|------------:|-----------:|------:|--------:|-------:|----------:|------------:|
| ToCreatedResponse_Success    | DefaultJob | Default        | Default     | Default     | 430.656 ns |   7.7861 ns |  6.9021 ns |  1.13 |    0.02 | 0.0286 |     480 B |        0.97 |
| ToNoContentResponse_Success  | DefaultJob | Default        | Default     | Default     |   8.185 ns |   0.2032 ns |  0.3450 ns |  0.02 |    0.00 | 0.0029 |      48 B |        0.10 |
| ToHttpApiResponse_Success    | DefaultJob | Default        | Default     | Default     | 384.530 ns |   4.2060 ns |  3.5122 ns |  1.01 |    0.02 | 0.0296 |     496 B |        1.00 |
| ToHttpApiResponse_Error      | DefaultJob | Default        | Default     | Default     | 833.709 ns |   9.6737 ns |  8.0780 ns |  2.19 |    0.04 | 0.0734 |    1232 B |        2.48 |
| ToApiGatewayResponse_Error   | DefaultJob | Default        | Default     | Default     | 811.533 ns |  15.2340 ns | 13.5046 ns |  2.13 |    0.04 | 0.0734 |    1232 B |        2.48 |
| ToApiGatewayResponse_Success | DefaultJob | Default        | Default     | Default     | 380.767 ns |   5.4647 ns |  5.1117 ns |  1.00 |    0.02 | 0.0296 |     496 B |        1.00 |
|                              |            |                |             |             |            |             |            |       |         |        |           |             |
| ToCreatedResponse_Success    | ShortRun   | 3              | 1           | 3           | 432.573 ns | 131.2302 ns |  7.1932 ns |  1.15 |    0.02 | 0.0286 |     480 B |        0.97 |
| ToNoContentResponse_Success  | ShortRun   | 3              | 1           | 3           |   7.205 ns |   4.8000 ns |  0.2631 ns |  0.02 |    0.00 | 0.0029 |      48 B |        0.10 |
| ToHttpApiResponse_Success    | ShortRun   | 3              | 1           | 3           | 384.380 ns |  19.1043 ns |  1.0472 ns |  1.02 |    0.01 | 0.0296 |     496 B |        1.00 |
| ToHttpApiResponse_Error      | ShortRun   | 3              | 1           | 3           | 818.505 ns | 256.1181 ns | 14.0387 ns |  2.18 |    0.03 | 0.0734 |    1232 B |        2.48 |
| ToApiGatewayResponse_Error   | ShortRun   | 3              | 1           | 3           | 804.969 ns | 145.1428 ns |  7.9558 ns |  2.14 |    0.02 | 0.0734 |    1232 B |        2.48 |
| ToApiGatewayResponse_Success | ShortRun   | 3              | 1           | 3           | 375.895 ns |  48.7465 ns |  2.6720 ns |  1.00 |    0.01 | 0.0296 |     496 B |        1.00 |
