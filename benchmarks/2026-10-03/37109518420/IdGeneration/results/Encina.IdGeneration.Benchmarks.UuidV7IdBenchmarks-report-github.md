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
| Generate_GetValue  | Job-YFEFPZ | 10             | Default     | 1,089.7 ns |   5.97 ns | 3.95 ns |  1.01 |         - |          NA |
| NewGuid_Comparison | Job-YFEFPZ | 10             | Default     |   608.4 ns |   0.79 ns | 0.52 ns |  0.56 |         - |          NA |
| Generate           | Job-YFEFPZ | 10             | Default     | 1,080.9 ns |   3.04 ns | 2.01 ns |  1.00 |         - |          NA |
|                    |            |                |             |            |           |         |       |           |             |
| Generate_GetValue  | ShortRun   | 3              | 1           | 1,078.6 ns |  13.39 ns | 0.73 ns |  1.00 |         - |          NA |
| NewGuid_Comparison | ShortRun   | 3              | 1           |   609.0 ns |   9.32 ns | 0.51 ns |  0.56 |         - |          NA |
| Generate           | ShortRun   | 3              | 1           | 1,082.7 ns | 117.34 ns | 6.43 ns |  1.00 |         - |          NA |
