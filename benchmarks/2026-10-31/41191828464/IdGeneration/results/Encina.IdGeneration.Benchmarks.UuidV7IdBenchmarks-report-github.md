```

BenchmarkDotNet v0.15.8, Linux Ubuntu 24.04.5 LTS (Noble Numbat)
AMD EPYC 7763 2.64GHz, 1 CPU, 4 logical and 2 physical cores
.NET SDK 10.0.403
  [Host]     : .NET 10.0.14 (10.0.14, 10.0.1426.52326), X64 RyuJIT x86-64-v3
  Job-YFEFPZ : .NET 10.0.14 (10.0.14, 10.0.1426.52326), X64 RyuJIT x86-64-v3
  ShortRun   : .NET 10.0.14 (10.0.14, 10.0.1426.52326), X64 RyuJIT x86-64-v3

WarmupCount=3  

```
| Method             | Job        | IterationCount | LaunchCount | Mean       | Error    | StdDev  | Ratio | Allocated | Alloc Ratio |
|------------------- |----------- |--------------- |------------ |-----------:|---------:|--------:|------:|----------:|------------:|
| Generate_GetValue  | Job-YFEFPZ | 10             | Default     | 1,121.3 ns | 11.24 ns | 7.43 ns |  0.94 |         - |          NA |
| NewGuid_Comparison | Job-YFEFPZ | 10             | Default     |   641.6 ns |  1.45 ns | 0.87 ns |  0.54 |         - |          NA |
| Generate           | Job-YFEFPZ | 10             | Default     | 1,186.8 ns |  7.35 ns | 4.38 ns |  1.00 |         - |          NA |
|                    |            |                |             |            |          |         |       |           |             |
| Generate_GetValue  | ShortRun   | 3              | 1           | 1,123.4 ns |  5.84 ns | 0.32 ns |  0.99 |         - |          NA |
| NewGuid_Comparison | ShortRun   | 3              | 1           |   645.5 ns |  7.37 ns | 0.40 ns |  0.57 |         - |          NA |
| Generate           | ShortRun   | 3              | 1           | 1,131.3 ns | 16.00 ns | 0.88 ns |  1.00 |         - |          NA |
