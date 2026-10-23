```

BenchmarkDotNet v0.15.8, Linux Ubuntu 24.04.5 LTS (Noble Numbat)
AMD EPYC 9V74 2.60GHz, 1 CPU, 4 logical and 2 physical cores
.NET SDK 10.0.402
  [Host]   : .NET 10.0.13 (10.0.13, 10.0.1326.50504), X64 RyuJIT x86-64-v4
  ShortRun : .NET 10.0.13 (10.0.13, 10.0.1326.50504), X64 RyuJIT x86-64-v4

Job=ShortRun  IterationCount=3  LaunchCount=1  
WarmupCount=3  

```
| Method                        | Mean     | Error     | StdDev    | Ratio | RatioSD | Gen0   | Gen1   | Allocated | Alloc Ratio |
|------------------------------ |---------:|----------:|----------:|------:|--------:|-------:|-------:|----------:|------------:|
| RemoveAsync                   | 2.644 μs | 0.4179 μs | 0.0229 μs |  0.78 |    0.06 | 0.0458 |      - |     784 B |        1.10 |
| SetWithSlidingExpirationAsync | 3.268 μs | 4.3119 μs | 0.2364 μs |  0.97 |    0.09 | 0.0420 | 0.0381 |     720 B |        1.01 |
| SetAsync                      | 3.397 μs | 5.2909 μs | 0.2900 μs |  1.00 |    0.11 | 0.0420 | 0.0381 |     712 B |        1.00 |
