```

BenchmarkDotNet v0.15.8, Linux Ubuntu 24.04.5 LTS (Noble Numbat)
AMD EPYC 9V45 4.30GHz, 1 CPU, 4 logical and 2 physical cores
.NET SDK 10.0.401
  [Host]     : .NET 10.0.12 (10.0.12, 10.0.1226.42308), X64 RyuJIT x86-64-v4
  Job-YFEFPZ : .NET 10.0.12 (10.0.12, 10.0.1226.42308), X64 RyuJIT x86-64-v4
  ShortRun   : .NET 10.0.12 (10.0.12, 10.0.1226.42308), X64 RyuJIT x86-64-v4

WarmupCount=3  

```
| Method             | Job        | IterationCount | LaunchCount | Mean     | Error     | StdDev   | Ratio | RatioSD | Allocated | Alloc Ratio |
|------------------- |----------- |--------------- |------------ |---------:|----------:|---------:|------:|--------:|----------:|------------:|
| Generate_GetValue  | Job-YFEFPZ | 10             | Default     | 705.5 ns |   8.45 ns |  5.59 ns |  1.03 |    0.02 |         - |          NA |
| NewGuid_Comparison | Job-YFEFPZ | 10             | Default     | 466.2 ns |  16.67 ns |  9.92 ns |  0.68 |    0.02 |         - |          NA |
| Generate           | Job-YFEFPZ | 10             | Default     | 687.1 ns |  27.20 ns | 16.18 ns |  1.00 |    0.03 |         - |          NA |
|                    |            |                |             |          |           |          |       |         |           |             |
| Generate_GetValue  | ShortRun   | 3              | 1           | 719.1 ns | 141.15 ns |  7.74 ns |  1.11 |    0.01 |         - |          NA |
| NewGuid_Comparison | ShortRun   | 3              | 1           | 427.2 ns |  64.42 ns |  3.53 ns |  0.66 |    0.01 |         - |          NA |
| Generate           | ShortRun   | 3              | 1           | 650.1 ns |  57.44 ns |  3.15 ns |  1.00 |    0.01 |         - |          NA |
