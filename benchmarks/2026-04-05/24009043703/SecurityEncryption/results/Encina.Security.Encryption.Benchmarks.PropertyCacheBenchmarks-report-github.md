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
| GetProperties_ColdCache     | Job-QKDGBD | 1               | 10             | Default     | Default     | 3           |    435.4 μs | 14.34 μs |  8.54 μs |  1.00 |    0.03 |   15.3 KB |        1.00 |
| GetProperties_WarmCache     | Job-QKDGBD | 1               | 10             | Default     | Default     | 3           |    445.9 μs | 14.29 μs |  8.50 μs |  1.02 |    0.03 |   15.3 KB |        1.00 |
| GetProperties_MultipleTypes | Job-QKDGBD | 1               | 10             | Default     | Default     | 3           |    992.1 μs | 27.98 μs | 18.51 μs |  2.28 |    0.06 |  31.02 KB |        2.03 |
| SetValue_CompiledSetter     | Job-QKDGBD | 1               | 10             | Default     | Default     | 3           |    453.9 μs | 14.60 μs |  9.66 μs |  1.04 |    0.03 |  15.34 KB |        1.00 |
| GetValue_CompiledGetter     | Job-QKDGBD | 1               | 10             | Default     | Default     | 3           |    442.2 μs | 26.27 μs | 17.37 μs |  1.02 |    0.04 |  15.34 KB |        1.00 |
|                             |            |                 |                |             |             |             |             |          |          |       |         |           |             |
| GetProperties_ColdCache     | Dry        | Default         | 1              | 1           | ColdStart   | 1           | 14,497.3 μs |       NA |  0.00 μs |  1.00 |    0.00 |  15.28 KB |        1.00 |
| GetProperties_WarmCache     | Dry        | Default         | 1              | 1           | ColdStart   | 1           | 14,645.2 μs |       NA |  0.00 μs |  1.01 |    0.00 |  15.28 KB |        1.00 |
| GetProperties_MultipleTypes | Dry        | Default         | 1              | 1           | ColdStart   | 1           | 14,879.4 μs |       NA |  0.00 μs |  1.03 |    0.00 |  30.98 KB |        2.03 |
| SetValue_CompiledSetter     | Dry        | Default         | 1              | 1           | ColdStart   | 1           | 14,459.5 μs |       NA |  0.00 μs |  1.00 |    0.00 |  15.32 KB |        1.00 |
| GetValue_CompiledGetter     | Dry        | Default         | 1              | 1           | ColdStart   | 1           | 14,583.5 μs |       NA |  0.00 μs |  1.01 |    0.00 |  16.75 KB |        1.10 |
