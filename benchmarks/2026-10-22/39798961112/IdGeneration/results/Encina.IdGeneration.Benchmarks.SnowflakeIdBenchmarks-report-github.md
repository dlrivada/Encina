```

BenchmarkDotNet v0.15.8, Linux Ubuntu 24.04.5 LTS (Noble Numbat)
AMD EPYC 9V74 2.60GHz, 1 CPU, 4 logical and 2 physical cores
.NET SDK 10.0.402
  [Host]     : .NET 10.0.13 (10.0.13, 10.0.1326.50504), X64 RyuJIT x86-64-v3
  Job-YFEFPZ : .NET 10.0.13 (10.0.13, 10.0.1326.50504), X64 RyuJIT x86-64-v3
  MediumRun  : .NET 10.0.13 (10.0.13, 10.0.1326.50504), X64 RyuJIT x86-64-v3


```
| Method               | Job        | IterationCount | LaunchCount | WarmupCount | Mean     | Error   | StdDev  | Median   | Ratio | Allocated | Alloc Ratio |
|--------------------- |----------- |--------------- |------------ |------------ |---------:|--------:|--------:|---------:|------:|----------:|------------:|
| Generate_WithShardId | Job-YFEFPZ | 10             | Default     | 3           | 241.9 ns | 0.09 ns | 0.06 ns | 241.9 ns |  1.00 |         - |          NA |
| GenerateAndGetValue  | Job-YFEFPZ | 10             | Default     | 3           | 241.9 ns | 0.03 ns | 0.02 ns | 241.9 ns |  1.00 |         - |          NA |
| Generate             | Job-YFEFPZ | 10             | Default     | 3           | 241.8 ns | 0.10 ns | 0.07 ns | 241.8 ns |  1.00 |         - |          NA |
| ExtractShardId       | Job-YFEFPZ | 10             | Default     | 3           | 240.0 ns | 0.03 ns | 0.01 ns | 240.0 ns |  0.99 |         - |          NA |
|                      |            |                |             |             |          |         |         |          |       |           |             |
| Generate_WithShardId | MediumRun  | 15             | 2           | 10          | 241.9 ns | 0.03 ns | 0.04 ns | 241.9 ns |  1.00 |         - |          NA |
| GenerateAndGetValue  | MediumRun  | 15             | 2           | 10          | 241.8 ns | 0.04 ns | 0.05 ns | 241.8 ns |  1.00 |         - |          NA |
| Generate             | MediumRun  | 15             | 2           | 10          | 241.9 ns | 0.04 ns | 0.05 ns | 241.9 ns |  1.00 |         - |          NA |
| ExtractShardId       | MediumRun  | 15             | 2           | 10          | 240.2 ns | 0.14 ns | 0.18 ns | 240.3 ns |  0.99 |         - |          NA |
