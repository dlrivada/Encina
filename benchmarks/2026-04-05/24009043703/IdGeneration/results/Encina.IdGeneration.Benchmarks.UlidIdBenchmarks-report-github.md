```

BenchmarkDotNet v0.15.8, Linux Ubuntu 24.04.4 LTS (Noble Numbat)
AMD EPYC 7763 2.45GHz, 1 CPU, 4 logical and 2 physical cores
.NET SDK 10.0.201
  [Host]     : .NET 10.0.5 (10.0.5, 10.0.526.15411), X64 RyuJIT x86-64-v3
  Job-YFEFPZ : .NET 10.0.5 (10.0.5, 10.0.526.15411), X64 RyuJIT x86-64-v3
  Dry        : .NET 10.0.5 (10.0.5, 10.0.526.15411), X64 RyuJIT x86-64-v3


```
| Method            | Job        | IterationCount | LaunchCount | RunStrategy | UnrollFactor | WarmupCount | Mean          | Error     | StdDev    | Ratio | Gen0   | Allocated | Alloc Ratio |
|------------------ |----------- |--------------- |------------ |------------ |------------- |------------ |--------------:|----------:|----------:|------:|-------:|----------:|------------:|
| Generate          | Job-YFEFPZ | 10             | Default     | Default     | 16           | 3           |      1.099 μs | 0.0043 μs | 0.0023 μs |  1.00 | 0.0019 |      40 B |        1.00 |
| Generate_ToString | Job-YFEFPZ | 10             | Default     | Default     | 16           | 3           |      1.169 μs | 0.0030 μs | 0.0016 μs |  1.06 | 0.0057 |     120 B |        3.00 |
| NewUlid_Direct    | Job-YFEFPZ | 10             | Default     | Default     | 16           | 3           |      1.070 μs | 0.0052 μs | 0.0034 μs |  0.97 | 0.0019 |      40 B |        1.00 |
|                   |            |                |             |             |              |             |               |           |           |       |        |           |             |
| Generate          | Dry        | 1              | 1           | ColdStart   | 1            | 1           | 13,956.053 μs |        NA | 0.0000 μs |  1.00 |      - |      40 B |        1.00 |
| Generate_ToString | Dry        | 1              | 1           | ColdStart   | 1            | 1           | 16,561.976 μs |        NA | 0.0000 μs |  1.19 |      - |     120 B |        3.00 |
| NewUlid_Direct    | Dry        | 1              | 1           | ColdStart   | 1            | 1           |  4,120.589 μs |        NA | 0.0000 μs |  0.30 |      - |      40 B |        1.00 |
