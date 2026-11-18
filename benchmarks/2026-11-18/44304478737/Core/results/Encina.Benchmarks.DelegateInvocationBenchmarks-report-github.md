```

BenchmarkDotNet v0.15.8, Linux Ubuntu 26.04.1 LTS (Resolute Raccoon)
AMD EPYC 7763 3.24GHz, 1 CPU, 4 logical and 2 physical cores
.NET SDK 10.0.404
  [Host]   : .NET 10.0.15 (10.0.15, 10.0.1526.53008), X64 RyuJIT x86-64-v3
  ShortRun : .NET 10.0.15 (10.0.15, 10.0.1526.53008), X64 RyuJIT x86-64-v3

Job=ShortRun  IterationCount=3  LaunchCount=1  
WarmupCount=3  

```
| Method                  | Mean         | Error         | StdDev     | Ratio    | RatioSD | Gen0   | Gen1   | Allocated | Alloc Ratio |
|------------------------ |-------------:|--------------:|-----------:|---------:|--------:|-------:|-------:|----------:|------------:|
| CompiledDelegate        |     43.96 ns |     10.333 ns |   0.566 ns |     1.53 |    0.02 | 0.0067 |      - |     112 B |        1.00 |
| DirectCall              |     28.71 ns |      4.515 ns |   0.247 ns |     1.00 |    0.01 | 0.0067 |      - |     112 B |        1.00 |
| ExpressionCompilation   | 89,200.91 ns | 12,270.145 ns | 672.568 ns | 3,106.66 |   30.86 | 0.2441 | 0.1221 |    5275 B |       47.10 |
| GenericTypeConstruction |    129.87 ns |     23.931 ns |   1.312 ns |     4.52 |    0.05 | 0.0105 |      - |     176 B |        1.57 |
| MethodInfoInvoke        |     81.72 ns |     12.393 ns |   0.679 ns |     2.85 |    0.03 | 0.0105 |      - |     176 B |        1.57 |
