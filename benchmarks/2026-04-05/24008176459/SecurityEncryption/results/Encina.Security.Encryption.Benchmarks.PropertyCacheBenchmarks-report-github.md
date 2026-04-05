```

BenchmarkDotNet v0.15.8, Linux Ubuntu 24.04.4 LTS (Noble Numbat)
AMD EPYC 7763 2.45GHz, 1 CPU, 4 logical and 2 physical cores
.NET SDK 10.0.201
  [Host]     : .NET 10.0.5 (10.0.5, 10.0.526.15411), X64 RyuJIT x86-64-v3
  Job-QKDGBD : .NET 10.0.5 (10.0.5, 10.0.526.15411), X64 RyuJIT x86-64-v3
  Dry        : .NET 10.0.5 (10.0.5, 10.0.526.15411), X64 RyuJIT x86-64-v3

UnrollFactor=1  

```
| Method                      | Job        | InvocationCount | IterationCount | LaunchCount | RunStrategy | WarmupCount | Mean        | Error    | StdDev   | Ratio | RatioSD | Allocated | Alloc Ratio |
|---------------------------- |----------- |---------------- |--------------- |------------ |------------ |------------ |------------:|---------:|---------:|------:|--------:|----------:|------------:|
| GetProperties_ColdCache     | Job-QKDGBD | 1               | 10             | Default     | Default     | 3           |    438.7 μs | 27.91 μs | 16.61 μs |  1.00 |    0.05 |   15.3 KB |        1.00 |
| GetProperties_WarmCache     | Job-QKDGBD | 1               | 10             | Default     | Default     | 3           |    426.1 μs | 11.45 μs |  6.81 μs |  0.97 |    0.04 |   15.3 KB |        1.00 |
| GetProperties_MultipleTypes | Job-QKDGBD | 1               | 10             | Default     | Default     | 3           |    994.4 μs | 37.71 μs | 24.94 μs |  2.27 |    0.10 |  31.02 KB |        2.03 |
| SetValue_CompiledSetter     | Job-QKDGBD | 1               | 10             | Default     | Default     | 3           |    436.3 μs | 26.19 μs | 17.32 μs |  1.00 |    0.05 |  15.34 KB |        1.00 |
| GetValue_CompiledGetter     | Job-QKDGBD | 1               | 10             | Default     | Default     | 3           |    454.4 μs | 21.82 μs | 14.43 μs |  1.04 |    0.05 |  15.34 KB |        1.00 |
|                             |            |                 |                |             |             |             |             |          |          |       |         |           |             |
| GetProperties_ColdCache     | Dry        | Default         | 1              | 1           | ColdStart   | 1           | 15,022.4 μs |       NA |  0.00 μs |  1.00 |    0.00 |  15.28 KB |        1.00 |
| GetProperties_WarmCache     | Dry        | Default         | 1              | 1           | ColdStart   | 1           | 14,970.9 μs |       NA |  0.00 μs |  1.00 |    0.00 |  15.28 KB |        1.00 |
| GetProperties_MultipleTypes | Dry        | Default         | 1              | 1           | ColdStart   | 1           | 15,674.0 μs |       NA |  0.00 μs |  1.04 |    0.00 |  30.98 KB |        2.03 |
| SetValue_CompiledSetter     | Dry        | Default         | 1              | 1           | ColdStart   | 1           | 14,787.1 μs |       NA |  0.00 μs |  0.98 |    0.00 |  15.32 KB |        1.00 |
| GetValue_CompiledGetter     | Dry        | Default         | 1              | 1           | ColdStart   | 1           | 15,343.7 μs |       NA |  0.00 μs |  1.02 |    0.00 |  16.75 KB |        1.10 |
