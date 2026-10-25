```

BenchmarkDotNet v0.15.8, Linux Ubuntu 24.04.5 LTS (Noble Numbat)
AMD EPYC 7763 3.02GHz, 1 CPU, 4 logical and 2 physical cores
.NET SDK 10.0.402
  [Host]    : .NET 10.0.13 (10.0.13, 10.0.1326.50504), X64 RyuJIT x86-64-v3
  MediumRun : .NET 10.0.13 (10.0.13, 10.0.1326.50504), X64 RyuJIT x86-64-v3

Job=MediumRun  IterationCount=15  LaunchCount=2  
WarmupCount=10  

```
| Method                        | Mean      | Error     | StdDev    | Ratio | RatioSD | Gen0   | Gen1   | Allocated | Alloc Ratio |
|------------------------------ |----------:|----------:|----------:|------:|--------:|-------:|-------:|----------:|------------:|
| RemoveAsync                   |  4.949 μs | 0.0979 μs | 0.1465 μs |  1.08 |    0.04 | 0.0610 |      - |   1.11 KB |        1.07 |
| RemoveByTagAsync              | 10.293 μs | 0.1714 μs | 0.2458 μs |  2.25 |    0.08 | 0.1373 | 0.1221 |   2.39 KB |        2.30 |
| SetAsync                      |  4.572 μs | 0.0859 μs | 0.1205 μs |  1.00 |    0.04 | 0.0610 | 0.0534 |   1.04 KB |        1.00 |
| SetWithSlidingExpirationAsync |  4.583 μs | 0.0767 μs | 0.1075 μs |  1.00 |    0.03 | 0.0763 | 0.0687 |   1.35 KB |        1.30 |
