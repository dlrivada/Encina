```

BenchmarkDotNet v0.15.8, Linux Ubuntu 26.04.1 LTS (Resolute Raccoon)
Intel Xeon Platinum 8370C CPU 2.80GHz (Max: 2.79GHz), 1 CPU, 4 logical and 2 physical cores
.NET SDK 10.0.403
  [Host]     : .NET 10.0.14 (10.0.14, 10.0.1426.52326), X64 RyuJIT x86-64-v4
  DefaultJob : .NET 10.0.14 (10.0.14, 10.0.1426.52326), X64 RyuJIT x86-64-v4
  ShortRun   : .NET 10.0.14 (10.0.14, 10.0.1426.52326), X64 RyuJIT x86-64-v4


```
| Method                       | Job        | IterationCount | LaunchCount | WarmupCount | Mean        | Error      | StdDev    | Ratio | Gen0   | Allocated | Alloc Ratio |
|----------------------------- |----------- |--------------- |------------ |------------ |------------:|-----------:|----------:|------:|-------:|----------:|------------:|
| ToCreatedResponse_Success    | DefaultJob | Default        | Default     | Default     |   850.59 ns |   4.235 ns |  3.755 ns |  1.18 | 0.0191 |     480 B |        0.97 |
| ToNoContentResponse_Success  | DefaultJob | Default        | Default     | Default     |    16.09 ns |   0.048 ns |  0.038 ns |  0.02 | 0.0019 |      48 B |        0.10 |
| ToHttpApiResponse_Success    | DefaultJob | Default        | Default     | Default     |   745.50 ns |   3.553 ns |  3.150 ns |  1.03 | 0.0191 |     496 B |        1.00 |
| ToHttpApiResponse_Error      | DefaultJob | Default        | Default     | Default     | 1,778.36 ns |   3.474 ns |  2.901 ns |  2.46 | 0.0477 |    1232 B |        2.48 |
| ToApiGatewayResponse_Error   | DefaultJob | Default        | Default     | Default     | 1,799.44 ns |   6.483 ns |  5.747 ns |  2.49 | 0.0477 |    1232 B |        2.48 |
| ToApiGatewayResponse_Success | DefaultJob | Default        | Default     | Default     |   723.79 ns |   4.290 ns |  3.803 ns |  1.00 | 0.0191 |     496 B |        1.00 |
|                              |            |                |             |             |             |            |           |       |        |           |             |
| ToCreatedResponse_Success    | ShortRun   | 3              | 1           | 3           |   822.73 ns |  49.395 ns |  2.708 ns |  1.09 | 0.0191 |     480 B |        0.97 |
| ToNoContentResponse_Success  | ShortRun   | 3              | 1           | 3           |    16.52 ns |   0.919 ns |  0.050 ns |  0.02 | 0.0019 |      48 B |        0.10 |
| ToHttpApiResponse_Success    | ShortRun   | 3              | 1           | 3           |   741.05 ns |  31.927 ns |  1.750 ns |  0.98 | 0.0191 |     496 B |        1.00 |
| ToHttpApiResponse_Error      | ShortRun   | 3              | 1           | 3           | 1,782.25 ns | 184.431 ns | 10.109 ns |  2.35 | 0.0477 |    1232 B |        2.48 |
| ToApiGatewayResponse_Error   | ShortRun   | 3              | 1           | 3           | 1,782.40 ns |  72.791 ns |  3.990 ns |  2.35 | 0.0477 |    1232 B |        2.48 |
| ToApiGatewayResponse_Success | ShortRun   | 3              | 1           | 3           |   758.18 ns |  49.354 ns |  2.705 ns |  1.00 | 0.0191 |     496 B |        1.00 |
