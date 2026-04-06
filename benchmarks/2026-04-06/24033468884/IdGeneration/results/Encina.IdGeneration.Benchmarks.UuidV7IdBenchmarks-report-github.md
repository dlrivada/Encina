```

BenchmarkDotNet v0.15.8, Linux Ubuntu 24.04.4 LTS (Noble Numbat)
AMD EPYC 7763 3.00GHz, 1 CPU, 4 logical and 2 physical cores
.NET SDK 10.0.201
  [Host]     : .NET 10.0.5 (10.0.5, 10.0.526.15411), X64 RyuJIT x86-64-v3
  Job-YFEFPZ : .NET 10.0.5 (10.0.5, 10.0.526.15411), X64 RyuJIT x86-64-v3
  MediumRun  : .NET 10.0.5 (10.0.5, 10.0.526.15411), X64 RyuJIT x86-64-v3


```
| Method             | Job        | IterationCount | LaunchCount | WarmupCount | Mean       | Error    | StdDev  | Ratio | Allocated | Alloc Ratio |
|------------------- |----------- |--------------- |------------ |------------ |-----------:|---------:|--------:|------:|----------:|------------:|
| Generate           | Job-YFEFPZ | 10             | Default     | 3           | 1,084.1 ns |  4.18 ns | 2.19 ns |  1.00 |         - |          NA |
| Generate_GetValue  | Job-YFEFPZ | 10             | Default     | 3           | 1,078.5 ns | 11.10 ns | 7.34 ns |  0.99 |         - |          NA |
| NewGuid_Comparison | Job-YFEFPZ | 10             | Default     | 3           |   605.8 ns |  1.30 ns | 0.78 ns |  0.56 |         - |          NA |
|                    |            |                |             |             |            |          |         |       |           |             |
| Generate           | MediumRun  | 15             | 2           | 10          | 1,087.3 ns |  4.52 ns | 6.77 ns |  1.00 |         - |          NA |
| Generate_GetValue  | MediumRun  | 15             | 2           | 10          | 1,074.2 ns |  4.25 ns | 6.36 ns |  0.99 |         - |          NA |
| NewGuid_Comparison | MediumRun  | 15             | 2           | 10          |   605.6 ns |  1.14 ns | 1.68 ns |  0.56 |         - |          NA |
