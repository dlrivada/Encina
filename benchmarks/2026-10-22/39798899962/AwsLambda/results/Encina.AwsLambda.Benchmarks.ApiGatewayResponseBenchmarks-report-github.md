```

BenchmarkDotNet v0.15.8, Linux Ubuntu 24.04.5 LTS (Noble Numbat)
AMD EPYC 9V45 4.26GHz, 1 CPU, 4 logical and 2 physical cores
.NET SDK 10.0.402
  [Host]     : .NET 10.0.13 (10.0.13, 10.0.1326.50504), X64 RyuJIT x86-64-v4
  DefaultJob : .NET 10.0.13 (10.0.13, 10.0.1326.50504), X64 RyuJIT x86-64-v4
  ShortRun   : .NET 10.0.13 (10.0.13, 10.0.1326.50504), X64 RyuJIT x86-64-v4


```
| Method                       | Job        | IterationCount | LaunchCount | WarmupCount | Mean       | Error       | StdDev     | Ratio | RatioSD | Gen0   | Allocated | Alloc Ratio |
|----------------------------- |----------- |--------------- |------------ |------------ |-----------:|------------:|-----------:|------:|--------:|-------:|----------:|------------:|
| ToCreatedResponse_Success    | DefaultJob | Default        | Default     | Default     | 438.702 ns |   8.4917 ns |  9.0860 ns |  1.05 |    0.03 | 0.0286 |     480 B |        0.97 |
| ToNoContentResponse_Success  | DefaultJob | Default        | Default     | Default     |   7.772 ns |   0.1959 ns |  0.2405 ns |  0.02 |    0.00 | 0.0029 |      48 B |        0.10 |
| ToHttpApiResponse_Success    | DefaultJob | Default        | Default     | Default     | 399.548 ns |   6.1401 ns |  5.7435 ns |  0.95 |    0.02 | 0.0296 |     496 B |        1.00 |
| ToHttpApiResponse_Error      | DefaultJob | Default        | Default     | Default     | 778.348 ns |  13.5254 ns | 12.6517 ns |  1.86 |    0.04 | 0.0734 |    1232 B |        2.48 |
| ToApiGatewayResponse_Error   | DefaultJob | Default        | Default     | Default     | 805.802 ns |  14.5553 ns | 18.4078 ns |  1.92 |    0.05 | 0.0734 |    1232 B |        2.48 |
| ToApiGatewayResponse_Success | DefaultJob | Default        | Default     | Default     | 418.901 ns |   7.8986 ns |  7.3884 ns |  1.00 |    0.02 | 0.0296 |     496 B |        1.00 |
|                              |            |                |             |             |            |             |            |       |         |        |           |             |
| ToCreatedResponse_Success    | ShortRun   | 3              | 1           | 3           | 434.671 ns | 201.3592 ns | 11.0372 ns |  1.08 |    0.02 | 0.0286 |     480 B |        0.97 |
| ToNoContentResponse_Success  | ShortRun   | 3              | 1           | 3           |   7.350 ns |   4.6793 ns |  0.2565 ns |  0.02 |    0.00 | 0.0029 |      48 B |        0.10 |
| ToHttpApiResponse_Success    | ShortRun   | 3              | 1           | 3           | 392.628 ns |  72.9534 ns |  3.9988 ns |  0.98 |    0.01 | 0.0296 |     496 B |        1.00 |
| ToHttpApiResponse_Error      | ShortRun   | 3              | 1           | 3           | 776.394 ns | 200.4271 ns | 10.9861 ns |  1.93 |    0.02 | 0.0734 |    1232 B |        2.48 |
| ToApiGatewayResponse_Error   | ShortRun   | 3              | 1           | 3           | 794.092 ns | 101.5632 ns |  5.5670 ns |  1.98 |    0.01 | 0.0734 |    1232 B |        2.48 |
| ToApiGatewayResponse_Success | ShortRun   | 3              | 1           | 3           | 401.846 ns |  12.6813 ns |  0.6951 ns |  1.00 |    0.00 | 0.0296 |     496 B |        1.00 |
