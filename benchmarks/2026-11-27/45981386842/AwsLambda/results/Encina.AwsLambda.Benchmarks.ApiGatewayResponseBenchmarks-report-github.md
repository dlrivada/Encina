```

BenchmarkDotNet v0.15.8, Linux Ubuntu 26.04.1 LTS (Resolute Raccoon)
AMD EPYC 9V74 3.69GHz, 1 CPU, 4 logical and 2 physical cores
.NET SDK 10.0.404
  [Host]     : .NET 10.0.15 (10.0.15, 10.0.1526.53008), X64 RyuJIT x86-64-v4
  DefaultJob : .NET 10.0.15 (10.0.15, 10.0.1526.53008), X64 RyuJIT x86-64-v4
  ShortRun   : .NET 10.0.15 (10.0.15, 10.0.1526.53008), X64 RyuJIT x86-64-v4


```
| Method                       | Job        | IterationCount | LaunchCount | WarmupCount | Mean        | Error      | StdDev   | Ratio | RatioSD | Gen0   | Allocated | Alloc Ratio |
|----------------------------- |----------- |--------------- |------------ |------------ |------------:|-----------:|---------:|------:|--------:|-------:|----------:|------------:|
| ToCreatedResponse_Success    | DefaultJob | Default        | Default     | Default     |   587.12 ns |   4.096 ns | 3.832 ns |  1.17 |    0.01 | 0.0286 |     480 B |        0.97 |
| ToNoContentResponse_Success  | DefaultJob | Default        | Default     | Default     |    10.96 ns |   0.243 ns | 0.215 ns |  0.02 |    0.00 | 0.0029 |      48 B |        0.10 |
| ToHttpApiResponse_Success    | DefaultJob | Default        | Default     | Default     |   484.96 ns |   4.142 ns | 3.875 ns |  0.96 |    0.01 | 0.0296 |     496 B |        1.00 |
| ToHttpApiResponse_Error      | DefaultJob | Default        | Default     | Default     | 1,154.57 ns |  10.745 ns | 9.525 ns |  2.29 |    0.02 | 0.0687 |    1176 B |        2.37 |
| ToApiGatewayResponse_Error   | DefaultJob | Default        | Default     | Default     | 1,134.79 ns |  11.228 ns | 9.953 ns |  2.25 |    0.02 | 0.0687 |    1176 B |        2.37 |
| ToApiGatewayResponse_Success | DefaultJob | Default        | Default     | Default     |   503.81 ns |   1.419 ns | 1.108 ns |  1.00 |    0.00 | 0.0296 |     496 B |        1.00 |
|                              |            |                |             |             |             |            |          |       |         |        |           |             |
| ToCreatedResponse_Success    | ShortRun   | 3              | 1           | 3           |   556.91 ns |  31.003 ns | 1.699 ns |  1.10 |    0.01 | 0.0286 |     480 B |        0.97 |
| ToNoContentResponse_Success  | ShortRun   | 3              | 1           | 3           |    10.84 ns |   2.199 ns | 0.121 ns |  0.02 |    0.00 | 0.0029 |      48 B |        0.10 |
| ToHttpApiResponse_Success    | ShortRun   | 3              | 1           | 3           |   519.33 ns |  78.840 ns | 4.322 ns |  1.03 |    0.01 | 0.0296 |     496 B |        1.00 |
| ToHttpApiResponse_Error      | ShortRun   | 3              | 1           | 3           | 1,113.48 ns |  99.272 ns | 5.441 ns |  2.21 |    0.02 | 0.0687 |    1176 B |        2.37 |
| ToApiGatewayResponse_Error   | ShortRun   | 3              | 1           | 3           | 1,125.03 ns | 156.914 ns | 8.601 ns |  2.23 |    0.02 | 0.0687 |    1176 B |        2.37 |
| ToApiGatewayResponse_Success | ShortRun   | 3              | 1           | 3           |   504.68 ns |  61.934 ns | 3.395 ns |  1.00 |    0.01 | 0.0296 |     496 B |        1.00 |
