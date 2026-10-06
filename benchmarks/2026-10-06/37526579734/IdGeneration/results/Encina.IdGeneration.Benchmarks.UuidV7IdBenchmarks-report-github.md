```

BenchmarkDotNet v0.15.8, Linux Ubuntu 24.04.5 LTS (Noble Numbat)
AMD EPYC 7763 2.45GHz, 1 CPU, 4 logical and 2 physical cores
.NET SDK 10.0.401
  [Host]     : .NET 10.0.12 (10.0.12, 10.0.1226.42308), X64 RyuJIT x86-64-v3
  Job-YFEFPZ : .NET 10.0.12 (10.0.12, 10.0.1226.42308), X64 RyuJIT x86-64-v3
  ShortRun   : .NET 10.0.12 (10.0.12, 10.0.1226.42308), X64 RyuJIT x86-64-v3

WarmupCount=3  

```
| Method             | Job        | IterationCount | LaunchCount | Mean       | Error     | StdDev  | Ratio | Allocated | Alloc Ratio |
|------------------- |----------- |--------------- |------------ |-----------:|----------:|--------:|------:|----------:|------------:|
| Generate_GetValue  | Job-YFEFPZ | 10             | Default     | 1,093.8 ns |  10.01 ns | 5.96 ns |  0.89 |         - |          NA |
| NewGuid_Comparison | Job-YFEFPZ | 10             | Default     |   607.7 ns |   0.86 ns | 0.51 ns |  0.49 |         - |          NA |
| Generate           | Job-YFEFPZ | 10             | Default     | 1,235.6 ns |   6.36 ns | 3.78 ns |  1.00 |         - |          NA |
|                    |            |                |             |            |           |         |       |           |             |
| Generate_GetValue  | ShortRun   | 3              | 1           | 1,090.0 ns | 163.13 ns | 8.94 ns |  1.01 |         - |          NA |
| NewGuid_Comparison | ShortRun   | 3              | 1           |   608.9 ns |   4.09 ns | 0.22 ns |  0.56 |         - |          NA |
| Generate           | ShortRun   | 3              | 1           | 1,082.3 ns |  94.28 ns | 5.17 ns |  1.00 |         - |          NA |
