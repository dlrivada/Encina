```

BenchmarkDotNet v0.15.8, Linux Ubuntu 24.04.5 LTS (Noble Numbat)
AMD EPYC 9V45 2.60GHz, 1 CPU, 4 logical and 2 physical cores
.NET SDK 10.0.402
  [Host]    : .NET 10.0.13 (10.0.13, 10.0.1326.50504), X64 RyuJIT x86-64-v4
  MediumRun : .NET 10.0.13 (10.0.13, 10.0.1326.50504), X64 RyuJIT x86-64-v4

Job=MediumRun  IterationCount=15  LaunchCount=2  
WarmupCount=10  

```
| Method       | Mean     | Error   | StdDev  | Ratio | RatioSD | Gen0   | Allocated | Alloc Ratio |
|------------- |---------:|--------:|--------:|------:|--------:|-------:|----------:|------------:|
| AddAsync     | 165.8 μs | 2.47 μs | 3.62 μs |  1.08 |    0.03 | 0.9766 |  16.02 KB |        1.41 |
| GetByIdAsync | 153.8 μs | 1.48 μs | 2.02 μs |  1.00 |    0.02 | 0.4883 |   11.4 KB |        1.00 |
