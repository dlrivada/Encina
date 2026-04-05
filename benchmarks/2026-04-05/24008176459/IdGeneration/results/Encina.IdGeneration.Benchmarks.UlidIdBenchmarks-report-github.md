```

BenchmarkDotNet v0.15.8, Linux Ubuntu 24.04.4 LTS (Noble Numbat)
AMD EPYC 7763 2.45GHz, 1 CPU, 4 logical and 2 physical cores
.NET SDK 10.0.201
  [Host]     : .NET 10.0.5 (10.0.5, 10.0.526.15411), X64 RyuJIT x86-64-v3
  Job-YFEFPZ : .NET 10.0.5 (10.0.5, 10.0.526.15411), X64 RyuJIT x86-64-v3
  Dry        : .NET 10.0.5 (10.0.5, 10.0.526.15411), X64 RyuJIT x86-64-v3


```
| Method            | Job        | IterationCount | LaunchCount | RunStrategy | UnrollFactor | WarmupCount | Mean          | Error     | StdDev    | Ratio | RatioSD | Gen0   | Allocated | Alloc Ratio |
|------------------ |----------- |--------------- |------------ |------------ |------------- |------------ |--------------:|----------:|----------:|------:|--------:|-------:|----------:|------------:|
| Generate          | Job-YFEFPZ | 10             | Default     | Default     | 16           | 3           |      1.103 μs | 0.0090 μs | 0.0047 μs |  1.00 |    0.01 | 0.0019 |      40 B |        1.00 |
| Generate_ToString | Job-YFEFPZ | 10             | Default     | Default     | 16           | 3           |      1.180 μs | 0.0257 μs | 0.0170 μs |  1.07 |    0.02 | 0.0057 |     120 B |        3.00 |
| NewUlid_Direct    | Job-YFEFPZ | 10             | Default     | Default     | 16           | 3           |      1.060 μs | 0.0066 μs | 0.0044 μs |  0.96 |    0.01 | 0.0019 |      40 B |        1.00 |
|                   |            |                |             |             |              |             |               |           |           |       |         |        |           |             |
| Generate          | Dry        | 1              | 1           | ColdStart   | 1            | 1           | 14,349.075 μs |        NA | 0.0000 μs |  1.00 |    0.00 |      - |      40 B |        1.00 |
| Generate_ToString | Dry        | 1              | 1           | ColdStart   | 1            | 1           | 17,222.963 μs |        NA | 0.0000 μs |  1.20 |    0.00 |      - |     120 B |        3.00 |
| NewUlid_Direct    | Dry        | 1              | 1           | ColdStart   | 1            | 1           |  4,276.195 μs |        NA | 0.0000 μs |  0.30 |    0.00 |      - |      40 B |        1.00 |
