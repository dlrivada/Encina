```

BenchmarkDotNet v0.15.8, Linux Ubuntu 26.04.1 LTS (Resolute Raccoon)
AMD EPYC 9V45 4.50GHz, 1 CPU, 4 logical and 2 physical cores
.NET SDK 10.0.404
  [Host]   : .NET 10.0.15 (10.0.15, 10.0.1526.53008), X64 RyuJIT x86-64-v4
  ShortRun : .NET 10.0.15 (10.0.15, 10.0.1526.53008), X64 RyuJIT x86-64-v4

Job=ShortRun  IterationCount=3  LaunchCount=1  
WarmupCount=3  

```
| Method                  | Mean         | Error        | StdDev     | Ratio    | RatioSD | Gen0   | Gen1   | Allocated | Alloc Ratio |
|------------------------ |-------------:|-------------:|-----------:|---------:|--------:|-------:|-------:|----------:|------------:|
| CompiledDelegate        |     20.28 ns |    10.012 ns |   0.549 ns |     1.30 |    0.04 | 0.0067 |      - |     112 B |        1.00 |
| DirectCall              |     15.63 ns |     5.161 ns |   0.283 ns |     1.00 |    0.02 | 0.0067 |      - |     112 B |        1.00 |
| ExpressionCompilation   | 17,077.72 ns | 4,935.548 ns | 270.534 ns | 1,093.13 |   22.65 | 0.3052 | 0.2747 |    5287 B |       47.21 |
| GenericTypeConstruction |     67.14 ns |    10.944 ns |   0.600 ns |     4.30 |    0.07 | 0.0105 |      - |     176 B |        1.57 |
| MethodInfoInvoke        |     44.95 ns |    10.206 ns |   0.559 ns |     2.88 |    0.05 | 0.0105 |      - |     176 B |        1.57 |
