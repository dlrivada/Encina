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
| ToCreatedResponse_Success    | DefaultJob | Default        | Default     | Default     | 395.967 ns |   1.9549 ns |  1.8286 ns |  1.03 |    0.02 | 0.0286 |     480 B |        0.97 |
| ToNoContentResponse_Success  | DefaultJob | Default        | Default     | Default     |   7.477 ns |   0.1781 ns |  0.1829 ns |  0.02 |    0.00 | 0.0029 |      48 B |        0.10 |
| ToHttpApiResponse_Success    | DefaultJob | Default        | Default     | Default     | 362.791 ns |   3.6409 ns |  3.4057 ns |  0.95 |    0.02 | 0.0296 |     496 B |        1.00 |
| ToHttpApiResponse_Error      | DefaultJob | Default        | Default     | Default     | 741.341 ns |   8.0436 ns |  7.5240 ns |  1.94 |    0.03 | 0.0734 |    1232 B |        2.48 |
| ToApiGatewayResponse_Error   | DefaultJob | Default        | Default     | Default     | 796.996 ns |   9.4477 ns |  8.8374 ns |  2.08 |    0.04 | 0.0734 |    1232 B |        2.48 |
| ToApiGatewayResponse_Success | DefaultJob | Default        | Default     | Default     | 382.886 ns |   5.9531 ns |  5.5685 ns |  1.00 |    0.02 | 0.0296 |     496 B |        1.00 |
|                              |            |                |             |             |            |             |            |       |         |        |           |             |
| ToCreatedResponse_Success    | ShortRun   | 3              | 1           | 3           | 396.985 ns |  26.0160 ns |  1.4260 ns |  1.03 |    0.00 | 0.0286 |     480 B |        0.97 |
| ToNoContentResponse_Success  | ShortRun   | 3              | 1           | 3           |   6.741 ns |   1.1489 ns |  0.0630 ns |  0.02 |    0.00 | 0.0029 |      48 B |        0.10 |
| ToHttpApiResponse_Success    | ShortRun   | 3              | 1           | 3           | 363.875 ns |  30.8340 ns |  1.6901 ns |  0.94 |    0.00 | 0.0296 |     496 B |        1.00 |
| ToHttpApiResponse_Error      | ShortRun   | 3              | 1           | 3           | 773.198 ns | 224.1739 ns | 12.2877 ns |  2.00 |    0.03 | 0.0734 |    1232 B |        2.48 |
| ToApiGatewayResponse_Error   | ShortRun   | 3              | 1           | 3           | 820.768 ns |  92.3397 ns |  5.0615 ns |  2.12 |    0.01 | 0.0734 |    1232 B |        2.48 |
| ToApiGatewayResponse_Success | ShortRun   | 3              | 1           | 3           | 386.304 ns |  11.4485 ns |  0.6275 ns |  1.00 |    0.00 | 0.0296 |     496 B |        1.00 |
