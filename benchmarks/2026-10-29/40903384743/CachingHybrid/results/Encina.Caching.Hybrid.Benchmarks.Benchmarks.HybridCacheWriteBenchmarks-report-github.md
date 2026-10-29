```

BenchmarkDotNet v0.15.8, Linux Ubuntu 24.04.5 LTS (Noble Numbat)
AMD EPYC 7763 3.22GHz, 1 CPU, 4 logical and 2 physical cores
.NET SDK 10.0.403
  [Host]   : .NET 10.0.14 (10.0.14, 10.0.1426.52326), X64 RyuJIT x86-64-v3
  ShortRun : .NET 10.0.14 (10.0.14, 10.0.1426.52326), X64 RyuJIT x86-64-v3

Job=ShortRun  IterationCount=3  LaunchCount=1  
WarmupCount=3  

```
| Method                        | Mean      | Error     | StdDev    | Ratio | RatioSD | Gen0   | Gen1   | Allocated | Alloc Ratio |
|------------------------------ |----------:|----------:|----------:|------:|--------:|-------:|-------:|----------:|------------:|
| RemoveAsync                   |  5.067 μs | 1.2545 μs | 0.0688 μs |  1.07 |    0.02 | 0.0610 |      - |   1.11 KB |        1.07 |
| RemoveByTagAsync              | 10.483 μs | 2.2912 μs | 0.1256 μs |  2.21 |    0.03 | 0.1678 | 0.1526 |   2.98 KB |        2.86 |
| SetAsync                      |  4.748 μs | 1.0198 μs | 0.0559 μs |  1.00 |    0.01 | 0.0610 | 0.0534 |   1.04 KB |        1.00 |
| SetWithSlidingExpirationAsync |  4.547 μs | 0.6968 μs | 0.0382 μs |  0.96 |    0.01 | 0.0610 | 0.0534 |   1.05 KB |        1.01 |
