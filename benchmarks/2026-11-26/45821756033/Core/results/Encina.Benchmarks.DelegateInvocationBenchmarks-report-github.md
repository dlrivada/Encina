```

BenchmarkDotNet v0.15.8, Linux Ubuntu 26.04.1 LTS (Resolute Raccoon)
Intel Xeon 6975P-C 2.70GHz, 1 CPU, 4 logical and 2 physical cores
.NET SDK 10.0.404
  [Host]   : .NET 10.0.15 (10.0.15, 10.0.1526.53008), X64 RyuJIT x86-64-v4
  ShortRun : .NET 10.0.15 (10.0.15, 10.0.1526.53008), X64 RyuJIT x86-64-v4

Job=ShortRun  IterationCount=3  LaunchCount=1  
WarmupCount=3  

```
| Method                  | Mean         | Error        | StdDev     | Ratio  | RatioSD | Gen0   | Gen1   | Allocated | Alloc Ratio |
|------------------------ |-------------:|-------------:|-----------:|-------:|--------:|-------:|-------:|----------:|------------:|
| CompiledDelegate        |     30.28 ns |     1.173 ns |   0.064 ns |   1.18 |    0.01 | 0.0013 |      - |     112 B |        1.00 |
| DirectCall              |     25.63 ns |     2.910 ns |   0.160 ns |   1.00 |    0.01 | 0.0013 |      - |     112 B |        1.00 |
| ExpressionCompilation   | 24,366.50 ns | 2,659.520 ns | 145.777 ns | 950.62 |    7.11 | 0.0610 | 0.0305 |    5287 B |       47.21 |
| GenericTypeConstruction |    100.42 ns |     4.606 ns |   0.252 ns |   3.92 |    0.02 | 0.0020 |      - |     176 B |        1.57 |
| MethodInfoInvoke        |     70.16 ns |     5.172 ns |   0.283 ns |   2.74 |    0.02 | 0.0020 |      - |     176 B |        1.57 |
