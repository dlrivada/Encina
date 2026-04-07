```

BenchmarkDotNet v0.15.8, Linux Ubuntu 24.04.4 LTS (Noble Numbat)
AMD EPYC 7763 2.45GHz, 1 CPU, 4 logical and 2 physical cores
.NET SDK 10.0.201
  [Host]     : .NET 10.0.5 (10.0.5, 10.0.526.15411), X64 RyuJIT x86-64-v3
  DefaultJob : .NET 10.0.5 (10.0.5, 10.0.526.15411), X64 RyuJIT x86-64-v3
  ShortRun   : .NET 10.0.5 (10.0.5, 10.0.526.15411), X64 RyuJIT x86-64-v3


```
| Method                       | Job        | IterationCount | LaunchCount | WarmupCount | Mean        | Error     | StdDev   | Ratio | Gen0   | Allocated | Alloc Ratio |
|----------------------------- |----------- |--------------- |------------ |------------ |------------:|----------:|---------:|------:|-------:|----------:|------------:|
| ToCreatedResponse_Success    | DefaultJob | Default        | Default     | Default     |   885.63 ns |  2.463 ns | 2.183 ns |  1.09 | 0.0286 |     480 B |        0.97 |
| ToNoContentResponse_Success  | DefaultJob | Default        | Default     | Default     |    13.46 ns |  0.023 ns | 0.020 ns |  0.02 | 0.0029 |      48 B |        0.10 |
| ToHttpApiResponse_Success    | DefaultJob | Default        | Default     | Default     |   820.23 ns |  1.377 ns | 1.220 ns |  1.01 | 0.0296 |     496 B |        1.00 |
| ToHttpApiResponse_Error      | DefaultJob | Default        | Default     | Default     | 1,416.28 ns |  4.177 ns | 3.703 ns |  1.75 | 0.0725 |    1232 B |        2.48 |
| ToApiGatewayResponse_Error   | DefaultJob | Default        | Default     | Default     | 1,428.60 ns |  3.150 ns | 2.792 ns |  1.76 | 0.0725 |    1232 B |        2.48 |
| ToApiGatewayResponse_Success | DefaultJob | Default        | Default     | Default     |   811.41 ns |  0.868 ns | 0.769 ns |  1.00 | 0.0296 |     496 B |        1.00 |
|                              |            |                |             |             |             |           |          |       |        |           |             |
| ToCreatedResponse_Success    | ShortRun   | 3              | 1           | 3           |   874.12 ns | 41.343 ns | 2.266 ns |  1.07 | 0.0286 |     480 B |        0.97 |
| ToNoContentResponse_Success  | ShortRun   | 3              | 1           | 3           |    13.54 ns |  0.756 ns | 0.041 ns |  0.02 | 0.0029 |      48 B |        0.10 |
| ToHttpApiResponse_Success    | ShortRun   | 3              | 1           | 3           |   794.41 ns | 43.576 ns | 2.389 ns |  0.97 | 0.0296 |     496 B |        1.00 |
| ToHttpApiResponse_Error      | ShortRun   | 3              | 1           | 3           | 1,480.51 ns | 43.940 ns | 2.409 ns |  1.81 | 0.0725 |    1232 B |        2.48 |
| ToApiGatewayResponse_Error   | ShortRun   | 3              | 1           | 3           | 1,439.72 ns | 55.303 ns | 3.031 ns |  1.76 | 0.0725 |    1232 B |        2.48 |
| ToApiGatewayResponse_Success | ShortRun   | 3              | 1           | 3           |   819.76 ns | 68.943 ns | 3.779 ns |  1.00 | 0.0296 |     496 B |        1.00 |
