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
| Generate_GetValue  | Job-YFEFPZ | 10             | Default     | 1,083.4 ns |  5.22 ns | 3.45 ns |  1.01 |         - |          NA |
| NewGuid_Comparison | Job-YFEFPZ | 10             | Default     |   607.8 ns |  0.47 ns | 0.25 ns |  0.56 |         - |          NA |
| Generate           | Job-YFEFPZ | 10             | Default     | 1,076.0 ns |  2.35 ns | 1.40 ns |  1.00 |         - |          NA |
|                    |            |                |             |            |          |         |       |           |             |
| Generate_GetValue  | ShortRun   | 3              | 1           | 1,079.0 ns |  1.33 ns | 0.07 ns |  0.94 |         - |          NA |
| NewGuid_Comparison | ShortRun   | 3              | 1           |   607.7 ns |  7.12 ns | 0.39 ns |  0.53 |         - |          NA |
| Generate           | ShortRun   | 3              | 1           | 1,151.6 ns | 83.28 ns | 4.56 ns |  1.00 |         - |          NA |
