```

BenchmarkDotNet v0.15.8, Linux Ubuntu 26.04.1 LTS (Resolute Raccoon)
AMD EPYC 9V45 4.27GHz, 1 CPU, 4 logical and 2 physical cores
.NET SDK 10.0.404
  [Host]    : .NET 10.0.15 (10.0.15, 10.0.1526.53008), X64 RyuJIT x86-64-v4
  MediumRun : .NET 10.0.15 (10.0.15, 10.0.1526.53008), X64 RyuJIT x86-64-v4

Job=MediumRun  IterationCount=15  LaunchCount=2  
WarmupCount=10  

```
| Method                        | Mean     | Error     | StdDev    | Ratio | RatioSD | Gen0   | Gen1   | Allocated | Alloc Ratio |
|------------------------------ |---------:|----------:|----------:|------:|--------:|-------:|-------:|----------:|------------:|
| RemoveAsync                   | 2.504 μs | 0.0138 μs | 0.0203 μs |  0.67 |    0.03 | 0.0648 |      - |   1.11 KB |        1.07 |
| RemoveByTagAsync              | 7.555 μs | 0.2157 μs | 0.3093 μs |  2.03 |    0.12 | 0.1450 | 0.1373 |   2.39 KB |        2.30 |
| SetAsync                      | 3.730 μs | 0.1187 μs | 0.1702 μs |  1.00 |    0.06 | 0.0610 | 0.0572 |   1.04 KB |        1.00 |
| SetWithSlidingExpirationAsync | 3.723 μs | 0.2604 μs | 0.3817 μs |  1.00 |    0.11 | 0.0610 | 0.0572 |   1.05 KB |        1.01 |
