```

BenchmarkDotNet v0.15.8, Linux Ubuntu 26.04.1 LTS (Resolute Raccoon)
AMD EPYC 7763 2.73GHz, 1 CPU, 4 logical and 2 physical cores
.NET SDK 10.0.403
  [Host]     : .NET 10.0.14 (10.0.14, 10.0.1426.52326), X64 RyuJIT x86-64-v3
  Job-YFEFPZ : .NET 10.0.14 (10.0.14, 10.0.1426.52326), X64 RyuJIT x86-64-v3
  MediumRun  : .NET 10.0.14 (10.0.14, 10.0.1426.52326), X64 RyuJIT x86-64-v3


```
| Method             | Job        | IterationCount | LaunchCount | WarmupCount | Mean     | Error   | StdDev  | Ratio | Allocated | Alloc Ratio |
|------------------- |----------- |--------------- |------------ |------------ |---------:|--------:|--------:|------:|----------:|------------:|
| Generate_GetValue  | Job-YFEFPZ | 10             | Default     | 3           | 767.9 ns | 3.77 ns | 2.24 ns |  1.00 |         - |          NA |
| NewGuid_Comparison | Job-YFEFPZ | 10             | Default     | 3           | 665.1 ns | 1.23 ns | 0.82 ns |  0.87 |         - |          NA |
| Generate           | Job-YFEFPZ | 10             | Default     | 3           | 768.1 ns | 1.51 ns | 0.79 ns |  1.00 |         - |          NA |
|                    |            |                |             |             |          |         |         |       |           |             |
| Generate_GetValue  | MediumRun  | 15             | 2           | 10          | 767.4 ns | 3.39 ns | 4.97 ns |  1.00 |         - |          NA |
| NewGuid_Comparison | MediumRun  | 15             | 2           | 10          | 663.8 ns | 0.35 ns | 0.48 ns |  0.86 |         - |          NA |
| Generate           | MediumRun  | 15             | 2           | 10          | 770.1 ns | 2.15 ns | 3.16 ns |  1.00 |         - |          NA |
