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
| Generate_GetValue  | Job-YFEFPZ | 10             | Default     | 1,165.0 ns |  2.99 ns | 1.98 ns |  1.07 |         - |          NA |
| NewGuid_Comparison | Job-YFEFPZ | 10             | Default     |   607.4 ns |  1.42 ns | 0.94 ns |  0.56 |         - |          NA |
| Generate           | Job-YFEFPZ | 10             | Default     | 1,086.3 ns |  2.79 ns | 1.66 ns |  1.00 |         - |          NA |
|                    |            |                |             |            |          |         |       |           |             |
| Generate_GetValue  | ShortRun   | 3              | 1           | 1,077.6 ns | 31.81 ns | 1.74 ns |  0.93 |         - |          NA |
| NewGuid_Comparison | ShortRun   | 3              | 1           |   609.0 ns |  9.65 ns | 0.53 ns |  0.52 |         - |          NA |
| Generate           | ShortRun   | 3              | 1           | 1,162.8 ns | 46.29 ns | 2.54 ns |  1.00 |         - |          NA |
