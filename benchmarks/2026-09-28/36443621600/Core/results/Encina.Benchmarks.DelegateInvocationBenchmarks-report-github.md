```

BenchmarkDotNet v0.15.8, Linux Ubuntu 24.04.5 LTS (Noble Numbat)
AMD EPYC 9V45 4.19GHz, 1 CPU, 4 logical and 2 physical cores
.NET SDK 10.0.401
  [Host]   : .NET 10.0.12 (10.0.12, 10.0.1226.42308), X64 RyuJIT x86-64-v4
  ShortRun : .NET 10.0.12 (10.0.12, 10.0.1226.42308), X64 RyuJIT x86-64-v4

Job=ShortRun  IterationCount=3  LaunchCount=1  
WarmupCount=3  

```
| Method                  | Mean         | Error        | StdDev     | Ratio  | RatioSD | Gen0   | Gen1   | Allocated | Alloc Ratio |
|------------------------ |-------------:|-------------:|-----------:|-------:|--------:|-------:|-------:|----------:|------------:|
| CompiledDelegate        |     23.30 ns |     3.693 ns |   0.202 ns |   1.28 |    0.01 | 0.0067 |      - |     112 B |        1.00 |
| DirectCall              |     18.26 ns |     2.268 ns |   0.124 ns |   1.00 |    0.01 | 0.0067 |      - |     112 B |        1.00 |
| ExpressionCompilation   | 18,144.33 ns | 6,270.275 ns | 343.695 ns | 993.79 |   17.32 | 0.3052 | 0.2747 |    5287 B |       47.21 |
| GenericTypeConstruction |     73.23 ns |    26.407 ns |   1.447 ns |   4.01 |    0.07 | 0.0105 |      - |     176 B |        1.57 |
| MethodInfoInvoke        |     49.24 ns |     6.370 ns |   0.349 ns |   2.70 |    0.02 | 0.0105 |      - |     176 B |        1.57 |
