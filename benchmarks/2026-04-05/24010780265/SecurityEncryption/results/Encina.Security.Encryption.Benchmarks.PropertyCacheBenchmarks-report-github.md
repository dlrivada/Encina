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
| GetProperties_ColdCache     | Job-QKDGBD | 1               | 10             | Default     | Default     | 3           |    441.9 μs | 24.79 μs | 16.39 μs |  1.00 |    0.05 |   15.3 KB |        1.00 |
| GetProperties_WarmCache     | Job-QKDGBD | 1               | 10             | Default     | Default     | 3           |    436.9 μs | 35.97 μs | 23.79 μs |  0.99 |    0.06 |   15.3 KB |        1.00 |
| GetProperties_MultipleTypes | Job-QKDGBD | 1               | 10             | Default     | Default     | 3           |  1,003.8 μs | 29.44 μs | 19.47 μs |  2.27 |    0.09 |  31.02 KB |        2.03 |
| SetValue_CompiledSetter     | Job-QKDGBD | 1               | 10             | Default     | Default     | 3           |    447.1 μs | 21.24 μs | 14.05 μs |  1.01 |    0.05 |  15.34 KB |        1.00 |
| GetValue_CompiledGetter     | Job-QKDGBD | 1               | 10             | Default     | Default     | 3           |    430.0 μs | 26.82 μs | 17.74 μs |  0.97 |    0.05 |  15.41 KB |        1.01 |
|                             |            |                 |                |             |             |             |             |          |          |       |         |           |             |
| GetProperties_ColdCache     | Dry        | Default         | 1              | 1           | ColdStart   | 1           | 14,769.0 μs |       NA |  0.00 μs |  1.00 |    0.00 |  15.28 KB |        1.00 |
| GetProperties_WarmCache     | Dry        | Default         | 1              | 1           | ColdStart   | 1           | 14,464.1 μs |       NA |  0.00 μs |  0.98 |    0.00 |  15.28 KB |        1.00 |
| GetProperties_MultipleTypes | Dry        | Default         | 1              | 1           | ColdStart   | 1           | 15,182.8 μs |       NA |  0.00 μs |  1.03 |    0.00 |  30.98 KB |        2.03 |
| SetValue_CompiledSetter     | Dry        | Default         | 1              | 1           | ColdStart   | 1           | 14,506.1 μs |       NA |  0.00 μs |  0.98 |    0.00 |  15.32 KB |        1.00 |
| GetValue_CompiledGetter     | Dry        | Default         | 1              | 1           | ColdStart   | 1           | 14,711.4 μs |       NA |  0.00 μs |  1.00 |    0.00 |  16.75 KB |        1.10 |
