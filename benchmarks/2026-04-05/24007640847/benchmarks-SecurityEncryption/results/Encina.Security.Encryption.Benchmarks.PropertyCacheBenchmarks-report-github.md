```

BenchmarkDotNet v0.15.8, Linux Ubuntu 24.04.4 LTS (Noble Numbat)
Intel Xeon Platinum 8370C CPU 2.80GHz, 1 CPU, 4 logical and 2 physical cores
.NET SDK 10.0.201
  [Host]     : .NET 10.0.5 (10.0.5, 10.0.526.15411), X64 RyuJIT x86-64-v4
  Job-QKDGBD : .NET 10.0.5 (10.0.5, 10.0.526.15411), X64 RyuJIT x86-64-v4
  Dry        : .NET 10.0.5 (10.0.5, 10.0.526.15411), X64 RyuJIT x86-64-v4

UnrollFactor=1  

```
| Method                      | Job        | InvocationCount | IterationCount | LaunchCount | RunStrategy | WarmupCount | Mean        | Error    | StdDev   | Ratio | RatioSD | Allocated | Alloc Ratio |
|---------------------------- |----------- |---------------- |--------------- |------------ |------------ |------------ |------------:|---------:|---------:|------:|--------:|----------:|------------:|
| GetProperties_ColdCache     | Job-QKDGBD | 1               | 10             | Default     | Default     | 3           |    457.5 μs | 28.93 μs | 19.14 μs |  1.00 |    0.06 |   15.3 KB |        1.00 |
| GetProperties_WarmCache     | Job-QKDGBD | 1               | 10             | Default     | Default     | 3           |    430.8 μs | 23.40 μs | 15.47 μs |  0.94 |    0.05 |   15.3 KB |        1.00 |
| GetProperties_MultipleTypes | Job-QKDGBD | 1               | 10             | Default     | Default     | 3           |    921.2 μs | 28.17 μs | 16.76 μs |  2.02 |    0.09 |  31.02 KB |        2.03 |
| SetValue_CompiledSetter     | Job-QKDGBD | 1               | 10             | Default     | Default     | 3           |    457.1 μs | 44.01 μs | 29.11 μs |  1.00 |    0.07 |  15.34 KB |        1.00 |
| GetValue_CompiledGetter     | Job-QKDGBD | 1               | 10             | Default     | Default     | 3           |    450.4 μs | 25.83 μs | 17.08 μs |  0.99 |    0.05 |  15.34 KB |        1.00 |
|                             |            |                 |                |             |             |             |             |          |          |       |         |           |             |
| GetProperties_ColdCache     | Dry        | Default         | 1              | 1           | ColdStart   | 1           | 13,869.2 μs |       NA |  0.00 μs |  1.00 |    0.00 |  15.28 KB |        1.00 |
| GetProperties_WarmCache     | Dry        | Default         | 1              | 1           | ColdStart   | 1           | 13,572.3 μs |       NA |  0.00 μs |  0.98 |    0.00 |  15.28 KB |        1.00 |
| GetProperties_MultipleTypes | Dry        | Default         | 1              | 1           | ColdStart   | 1           | 14,359.1 μs |       NA |  0.00 μs |  1.04 |    0.00 |  30.98 KB |        2.03 |
| SetValue_CompiledSetter     | Dry        | Default         | 1              | 1           | ColdStart   | 1           | 13,840.0 μs |       NA |  0.00 μs |  1.00 |    0.00 |  15.32 KB |        1.00 |
| GetValue_CompiledGetter     | Dry        | Default         | 1              | 1           | ColdStart   | 1           | 13,967.4 μs |       NA |  0.00 μs |  1.01 |    0.00 |  16.75 KB |        1.10 |
