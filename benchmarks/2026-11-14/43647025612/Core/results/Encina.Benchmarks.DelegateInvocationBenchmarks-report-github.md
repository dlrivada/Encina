```

BenchmarkDotNet v0.15.8, Linux Ubuntu 26.04.1 LTS (Resolute Raccoon)
AMD EPYC 9V45 2.60GHz, 1 CPU, 4 logical and 2 physical cores
.NET SDK 10.0.404
  [Host]   : .NET 10.0.15 (10.0.15, 10.0.1526.53008), X64 RyuJIT x86-64-v4
  ShortRun : .NET 10.0.15 (10.0.15, 10.0.1526.53008), X64 RyuJIT x86-64-v4

Job=ShortRun  IterationCount=3  LaunchCount=1  
WarmupCount=3  

```
| Method                  | Mean         | Error         | StdDev     | Ratio    | RatioSD | Gen0   | Gen1   | Allocated | Alloc Ratio |
|------------------------ |-------------:|--------------:|-----------:|---------:|--------:|-------:|-------:|----------:|------------:|
| CompiledDelegate        |     21.83 ns |      6.341 ns |   0.348 ns |     1.32 |    0.03 | 0.0067 |      - |     112 B |        1.00 |
| DirectCall              |     16.60 ns |      6.036 ns |   0.331 ns |     1.00 |    0.02 | 0.0067 |      - |     112 B |        1.00 |
| ExpressionCompilation   | 18,479.31 ns | 13,853.180 ns | 759.340 ns | 1,113.75 |   44.15 | 0.3052 | 0.2747 |    5287 B |       47.21 |
| GenericTypeConstruction |     72.80 ns |     24.106 ns |   1.321 ns |     4.39 |    0.10 | 0.0105 |      - |     176 B |        1.57 |
| MethodInfoInvoke        |     49.76 ns |     44.792 ns |   2.455 ns |     3.00 |    0.14 | 0.0105 |      - |     176 B |        1.57 |
