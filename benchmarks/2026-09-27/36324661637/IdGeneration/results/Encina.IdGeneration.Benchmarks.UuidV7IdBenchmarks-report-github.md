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
| Generate_GetValue  | Job-YFEFPZ | 10             | Default     | 1,096.6 ns |   6.43 ns | 4.25 ns |  1.01 |         - |          NA |
| NewGuid_Comparison | Job-YFEFPZ | 10             | Default     |   607.7 ns |   1.35 ns | 0.80 ns |  0.56 |         - |          NA |
| Generate           | Job-YFEFPZ | 10             | Default     | 1,087.6 ns |   3.94 ns | 2.60 ns |  1.00 |         - |          NA |
|                    |            |                |             |            |           |         |       |           |             |
| Generate_GetValue  | ShortRun   | 3              | 1           | 1,170.7 ns |  39.45 ns | 2.16 ns |  1.07 |         - |          NA |
| NewGuid_Comparison | ShortRun   | 3              | 1           |   606.8 ns |   3.98 ns | 0.22 ns |  0.56 |         - |          NA |
| Generate           | ShortRun   | 3              | 1           | 1,090.3 ns | 172.77 ns | 9.47 ns |  1.00 |         - |          NA |
