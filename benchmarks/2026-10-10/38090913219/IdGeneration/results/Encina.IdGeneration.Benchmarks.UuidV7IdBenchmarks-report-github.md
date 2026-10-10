```

BenchmarkDotNet v0.15.8, Linux Ubuntu 24.04.5 LTS (Noble Numbat)
AMD EPYC 9V74 2.60GHz, 1 CPU, 4 logical and 2 physical cores
.NET SDK 10.0.401
  [Host]     : .NET 10.0.12 (10.0.12, 10.0.1226.42308), X64 RyuJIT x86-64-v4
  Job-YFEFPZ : .NET 10.0.12 (10.0.12, 10.0.1226.42308), X64 RyuJIT x86-64-v4
  ShortRun   : .NET 10.0.12 (10.0.12, 10.0.1226.42308), X64 RyuJIT x86-64-v4

WarmupCount=3  

```
| Method             | Job        | IterationCount | LaunchCount | Mean     | Error     | StdDev  | Ratio | Allocated | Alloc Ratio |
|------------------- |----------- |--------------- |------------ |---------:|----------:|--------:|------:|----------:|------------:|
| Generate_GetValue  | Job-YFEFPZ | 10             | Default     | 976.2 ns |   5.86 ns | 3.06 ns |  1.06 |         - |          NA |
| NewGuid_Comparison | Job-YFEFPZ | 10             | Default     | 564.9 ns |   8.25 ns | 5.46 ns |  0.61 |         - |          NA |
| Generate           | Job-YFEFPZ | 10             | Default     | 925.1 ns |  11.48 ns | 6.83 ns |  1.00 |         - |          NA |
|                    |            |                |             |          |           |         |       |           |             |
| Generate_GetValue  | ShortRun   | 3              | 1           | 907.1 ns |  27.16 ns | 1.49 ns |  0.98 |         - |          NA |
| NewGuid_Comparison | ShortRun   | 3              | 1           | 559.1 ns |  23.50 ns | 1.29 ns |  0.61 |         - |          NA |
| Generate           | ShortRun   | 3              | 1           | 921.1 ns | 181.58 ns | 9.95 ns |  1.00 |         - |          NA |
