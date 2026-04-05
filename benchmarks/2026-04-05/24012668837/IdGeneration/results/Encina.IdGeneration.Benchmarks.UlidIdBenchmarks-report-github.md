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
| Generate          | Job-YFEFPZ | 10             | Default     | Default     | 16           | 3           |      1.120 μs | 0.0049 μs | 0.0032 μs |  1.00 | 0.0019 |      40 B |        1.00 |
| Generate_ToString | Job-YFEFPZ | 10             | Default     | Default     | 16           | 3           |      1.186 μs | 0.0053 μs | 0.0031 μs |  1.06 | 0.0057 |     120 B |        3.00 |
| NewUlid_Direct    | Job-YFEFPZ | 10             | Default     | Default     | 16           | 3           |      1.144 μs | 0.0041 μs | 0.0025 μs |  1.02 | 0.0019 |      40 B |        1.00 |
|                   |            |                |             |             |              |             |               |           |           |       |        |           |             |
| Generate          | Dry        | 1              | 1           | ColdStart   | 1            | 1           | 13,716.157 μs |        NA | 0.0000 μs |  1.00 |      - |      40 B |        1.00 |
| Generate_ToString | Dry        | 1              | 1           | ColdStart   | 1            | 1           | 16,177.763 μs |        NA | 0.0000 μs |  1.18 |      - |     120 B |        3.00 |
| NewUlid_Direct    | Dry        | 1              | 1           | ColdStart   | 1            | 1           |  3,971.981 μs |        NA | 0.0000 μs |  0.29 |      - |      40 B |        1.00 |
