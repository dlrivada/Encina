```

BenchmarkDotNet v0.15.8, Linux Ubuntu 24.04.4 LTS (Noble Numbat)
Intel Xeon Platinum 8370C CPU 2.80GHz (Max: 2.79GHz), 1 CPU, 4 logical and 2 physical cores
.NET SDK 10.0.201
  [Host]     : .NET 10.0.5 (10.0.5, 10.0.526.15411), X64 RyuJIT x86-64-v4
  Job-YFEFPZ : .NET 10.0.5 (10.0.5, 10.0.526.15411), X64 RyuJIT x86-64-v4
  Dry        : .NET 10.0.5 (10.0.5, 10.0.526.15411), X64 RyuJIT x86-64-v4


```
| Method            | Job        | IterationCount | LaunchCount | RunStrategy | UnrollFactor | WarmupCount | Mean            | Error   | StdDev  | Ratio | Gen0   | Allocated | Alloc Ratio |
|------------------ |----------- |--------------- |------------ |------------ |------------- |------------ |----------------:|--------:|--------:|------:|-------:|----------:|------------:|
| Generate          | Job-YFEFPZ | 10             | Default     | Default     | 16           | 3           |        960.1 ns | 2.85 ns | 1.70 ns |  1.00 |      - |      40 B |        1.00 |
| Generate_ToString | Job-YFEFPZ | 10             | Default     | Default     | 16           | 3           |      1,081.9 ns | 9.74 ns | 6.44 ns |  1.13 | 0.0038 |     120 B |        3.00 |
| NewUlid_Direct    | Job-YFEFPZ | 10             | Default     | Default     | 16           | 3           |        925.6 ns | 2.90 ns | 1.51 ns |  0.96 | 0.0010 |      40 B |        1.00 |
|                   |            |                |             |             |              |             |                 |         |         |       |        |           |             |
| Generate          | Dry        | 1              | 1           | ColdStart   | 1            | 1           | 12,617,939.0 ns |      NA | 0.00 ns |  1.00 |      - |      40 B |        1.00 |
| Generate_ToString | Dry        | 1              | 1           | ColdStart   | 1            | 1           | 15,102,317.0 ns |      NA | 0.00 ns |  1.20 |      - |     120 B |        3.00 |
| NewUlid_Direct    | Dry        | 1              | 1           | ColdStart   | 1            | 1           |  3,929,916.0 ns |      NA | 0.00 ns |  0.31 |      - |      40 B |        1.00 |
