```

BenchmarkDotNet v0.15.8, Linux Ubuntu 26.04.1 LTS (Resolute Raccoon)
Intel Xeon Platinum 8370C CPU 2.80GHz (Max: 2.79GHz), 1 CPU, 4 logical and 2 physical cores
.NET SDK 10.0.404
  [Host]     : .NET 10.0.15 (10.0.15, 10.0.1526.53008), X64 RyuJIT x86-64-v4
  DefaultJob : .NET 10.0.15 (10.0.15, 10.0.1526.53008), X64 RyuJIT x86-64-v4
  ShortRun   : .NET 10.0.15 (10.0.15, 10.0.1526.53008), X64 RyuJIT x86-64-v4


```
| Method                       | Job        | IterationCount | LaunchCount | WarmupCount | Mean        | Error      | StdDev    | Ratio | RatioSD | Gen0   | Allocated | Alloc Ratio |
|----------------------------- |----------- |--------------- |------------ |------------ |------------:|-----------:|----------:|------:|--------:|-------:|----------:|------------:|
| ToCreatedResponse_Success    | DefaultJob | Default        | Default     | Default     |   884.08 ns |   2.421 ns |  1.891 ns |  1.13 |    0.00 | 0.0191 |     480 B |        0.97 |
| ToNoContentResponse_Success  | DefaultJob | Default        | Default     | Default     |    15.77 ns |   0.391 ns |  0.495 ns |  0.02 |    0.00 | 0.0019 |      48 B |        0.10 |
| ToHttpApiResponse_Success    | DefaultJob | Default        | Default     | Default     |   791.98 ns |   5.027 ns |  4.702 ns |  1.02 |    0.01 | 0.0191 |     496 B |        1.00 |
| ToHttpApiResponse_Error      | DefaultJob | Default        | Default     | Default     | 1,736.89 ns |   7.502 ns |  6.264 ns |  2.23 |    0.01 | 0.0477 |    1232 B |        2.48 |
| ToApiGatewayResponse_Error   | DefaultJob | Default        | Default     | Default     | 1,804.62 ns |  14.887 ns | 13.925 ns |  2.32 |    0.02 | 0.0477 |    1232 B |        2.48 |
| ToApiGatewayResponse_Success | DefaultJob | Default        | Default     | Default     |   779.18 ns |   3.282 ns |  3.070 ns |  1.00 |    0.01 | 0.0191 |     496 B |        1.00 |
|                              |            |                |             |             |             |            |           |       |         |        |           |             |
| ToCreatedResponse_Success    | ShortRun   | 3              | 1           | 3           |   872.80 ns |  71.776 ns |  3.934 ns |  1.12 |    0.01 | 0.0191 |     480 B |        0.97 |
| ToNoContentResponse_Success  | ShortRun   | 3              | 1           | 3           |    15.05 ns |   5.023 ns |  0.275 ns |  0.02 |    0.00 | 0.0019 |      48 B |        0.10 |
| ToHttpApiResponse_Success    | ShortRun   | 3              | 1           | 3           |   732.69 ns |  81.329 ns |  4.458 ns |  0.94 |    0.01 | 0.0191 |     496 B |        1.00 |
| ToHttpApiResponse_Error      | ShortRun   | 3              | 1           | 3           | 1,744.25 ns | 104.472 ns |  5.726 ns |  2.23 |    0.01 | 0.0477 |    1232 B |        2.48 |
| ToApiGatewayResponse_Error   | ShortRun   | 3              | 1           | 3           | 1,766.75 ns |  59.476 ns |  3.260 ns |  2.26 |    0.01 | 0.0477 |    1232 B |        2.48 |
| ToApiGatewayResponse_Success | ShortRun   | 3              | 1           | 3           |   780.48 ns |  78.582 ns |  4.307 ns |  1.00 |    0.01 | 0.0191 |     496 B |        1.00 |
