```

BenchmarkDotNet v0.15.8, Linux Ubuntu 24.04.5 LTS (Noble Numbat)
AMD EPYC 7763 2.45GHz, 1 CPU, 4 logical and 2 physical cores
.NET SDK 10.0.401
  [Host]     : .NET 10.0.12 (10.0.12, 10.0.1226.42308), X64 RyuJIT x86-64-v3
  Job-YFEFPZ : .NET 10.0.12 (10.0.12, 10.0.1226.42308), X64 RyuJIT x86-64-v3
  ShortRun   : .NET 10.0.12 (10.0.12, 10.0.1226.42308), X64 RyuJIT x86-64-v3

WarmupCount=3  

```
| Method             | Job        | IterationCount | LaunchCount | Mean       | Error    | StdDev  | Ratio | Allocated | Alloc Ratio |
|------------------- |----------- |--------------- |------------ |-----------:|---------:|--------:|------:|----------:|------------:|
| Generate_GetValue  | Job-YFEFPZ | 10             | Default     | 1,094.4 ns |  5.93 ns | 3.92 ns |  0.94 |         - |          NA |
| NewGuid_Comparison | Job-YFEFPZ | 10             | Default     |   605.8 ns |  3.75 ns | 2.48 ns |  0.52 |         - |          NA |
| Generate           | Job-YFEFPZ | 10             | Default     | 1,167.6 ns |  3.23 ns | 2.13 ns |  1.00 |         - |          NA |
|                    |            |                |             |            |          |         |       |           |             |
| Generate_GetValue  | ShortRun   | 3              | 1           | 1,161.0 ns | 80.45 ns | 4.41 ns |  1.07 |         - |          NA |
| NewGuid_Comparison | ShortRun   | 3              | 1           |   605.0 ns |  3.34 ns | 0.18 ns |  0.56 |         - |          NA |
| Generate           | ShortRun   | 3              | 1           | 1,089.0 ns | 83.72 ns | 4.59 ns |  1.00 |         - |          NA |
