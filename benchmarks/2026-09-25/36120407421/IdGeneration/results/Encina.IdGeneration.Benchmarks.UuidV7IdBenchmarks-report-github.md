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
| Generate_GetValue  | Job-YFEFPZ | 10             | Default     | 1,087.1 ns |  8.41 ns | 5.56 ns |  1.01 |         - |          NA |
| NewGuid_Comparison | Job-YFEFPZ | 10             | Default     |   611.7 ns |  2.92 ns | 1.53 ns |  0.57 |         - |          NA |
| Generate           | Job-YFEFPZ | 10             | Default     | 1,080.4 ns |  6.53 ns | 3.88 ns |  1.00 |         - |          NA |
|                    |            |                |             |            |          |         |       |           |             |
| Generate_GetValue  | ShortRun   | 3              | 1           | 1,083.6 ns | 31.46 ns | 1.72 ns |  0.99 |         - |          NA |
| NewGuid_Comparison | ShortRun   | 3              | 1           |   609.2 ns |  4.33 ns | 0.24 ns |  0.56 |         - |          NA |
| Generate           | ShortRun   | 3              | 1           | 1,093.7 ns | 32.84 ns | 1.80 ns |  1.00 |         - |          NA |
