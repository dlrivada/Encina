```

BenchmarkDotNet v0.15.8, Linux Ubuntu 24.04.5 LTS (Noble Numbat)
AMD EPYC 9V74 3.69GHz, 1 CPU, 4 logical and 2 physical cores
.NET SDK 10.0.403
  [Host]    : .NET 10.0.14 (10.0.14, 10.0.1426.52326), X64 RyuJIT x86-64-v4
  MediumRun : .NET 10.0.14 (10.0.14, 10.0.1426.52326), X64 RyuJIT x86-64-v4

Job=MediumRun  IterationCount=15  LaunchCount=2  
WarmupCount=10  

```
| Method       | Mean     | Error   | StdDev  | Ratio | Gen0   | Allocated | Alloc Ratio |
|------------- |---------:|--------:|--------:|------:|-------:|----------:|------------:|
| AddAsync     | 324.4 μs | 1.03 μs | 1.48 μs |  0.94 | 0.9766 |  20.55 KB |        0.91 |
| GetByIdAsync | 346.5 μs | 1.66 μs | 2.43 μs |  1.00 | 0.9766 |  22.53 KB |        1.00 |
