```

BenchmarkDotNet v0.15.8, Linux Ubuntu 26.04.1 LTS (Resolute Raccoon)
AMD EPYC 9V74 3.68GHz, 1 CPU, 4 logical and 2 physical cores
.NET SDK 10.0.404
  [Host]     : .NET 10.0.15 (10.0.15, 10.0.1526.53008), X64 RyuJIT x86-64-v4
  DefaultJob : .NET 10.0.15 (10.0.15, 10.0.1526.53008), X64 RyuJIT x86-64-v4
  ShortRun   : .NET 10.0.15 (10.0.15, 10.0.1526.53008), X64 RyuJIT x86-64-v4


```
| Method                       | Job        | IterationCount | LaunchCount | WarmupCount | Mean        | Error      | StdDev    | Ratio | RatioSD | Gen0   | Allocated | Alloc Ratio |
|----------------------------- |----------- |--------------- |------------ |------------ |------------:|-----------:|----------:|------:|--------:|-------:|----------:|------------:|
| ToCreatedResponse_Success    | DefaultJob | Default        | Default     | Default     |   589.70 ns |   4.520 ns |  4.228 ns |  1.20 |    0.01 | 0.0286 |     480 B |        0.97 |
| ToNoContentResponse_Success  | DefaultJob | Default        | Default     | Default     |    10.92 ns |   0.195 ns |  0.182 ns |  0.02 |    0.00 | 0.0029 |      48 B |        0.10 |
| ToHttpApiResponse_Success    | DefaultJob | Default        | Default     | Default     |   497.08 ns |   2.962 ns |  2.626 ns |  1.01 |    0.01 | 0.0296 |     496 B |        1.00 |
| ToHttpApiResponse_Error      | DefaultJob | Default        | Default     | Default     | 1,151.64 ns |  12.406 ns | 10.360 ns |  2.33 |    0.03 | 0.0725 |    1232 B |        2.48 |
| ToApiGatewayResponse_Error   | DefaultJob | Default        | Default     | Default     | 1,113.66 ns |  14.616 ns | 13.671 ns |  2.26 |    0.03 | 0.0725 |    1232 B |        2.48 |
| ToApiGatewayResponse_Success | DefaultJob | Default        | Default     | Default     |   493.34 ns |   4.367 ns |  4.085 ns |  1.00 |    0.01 | 0.0296 |     496 B |        1.00 |
|                              |            |                |             |             |             |            |           |       |         |        |           |             |
| ToCreatedResponse_Success    | ShortRun   | 3              | 1           | 3           |   564.65 ns |  42.886 ns |  2.351 ns |  1.16 |    0.00 | 0.0286 |     480 B |        0.97 |
| ToNoContentResponse_Success  | ShortRun   | 3              | 1           | 3           |    10.90 ns |   1.148 ns |  0.063 ns |  0.02 |    0.00 | 0.0029 |      48 B |        0.10 |
| ToHttpApiResponse_Success    | ShortRun   | 3              | 1           | 3           |   501.52 ns |  97.581 ns |  5.349 ns |  1.03 |    0.01 | 0.0296 |     496 B |        1.00 |
| ToHttpApiResponse_Error      | ShortRun   | 3              | 1           | 3           | 1,122.10 ns | 327.816 ns | 17.969 ns |  2.30 |    0.03 | 0.0725 |    1232 B |        2.48 |
| ToApiGatewayResponse_Error   | ShortRun   | 3              | 1           | 3           | 1,142.95 ns | 237.842 ns | 13.037 ns |  2.35 |    0.02 | 0.0725 |    1232 B |        2.48 |
| ToApiGatewayResponse_Success | ShortRun   | 3              | 1           | 3           |   487.13 ns |  14.102 ns |  0.773 ns |  1.00 |    0.00 | 0.0296 |     496 B |        1.00 |
