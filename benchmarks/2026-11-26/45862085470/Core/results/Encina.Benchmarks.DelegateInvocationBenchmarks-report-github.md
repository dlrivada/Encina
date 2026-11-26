```

BenchmarkDotNet v0.15.8, Linux Ubuntu 26.04.1 LTS (Resolute Raccoon)
Intel Xeon 6975P-C 3.43GHz, 1 CPU, 4 logical and 2 physical cores
.NET SDK 10.0.404
  [Host]   : .NET 10.0.15 (10.0.15, 10.0.1526.53008), X64 RyuJIT x86-64-v4
  ShortRun : .NET 10.0.15 (10.0.15, 10.0.1526.53008), X64 RyuJIT x86-64-v4

Job=ShortRun  IterationCount=3  LaunchCount=1  
WarmupCount=3  

```
| Method                  | Mean         | Error        | StdDev     | Ratio    | RatioSD | Gen0   | Gen1   | Allocated | Alloc Ratio |
|------------------------ |-------------:|-------------:|-----------:|---------:|--------:|-------:|-------:|----------:|------------:|
| CompiledDelegate        |     27.54 ns |     3.343 ns |   0.183 ns |     1.23 |    0.03 | 0.0013 |      - |     112 B |        1.00 |
| DirectCall              |     22.44 ns |     9.754 ns |   0.535 ns |     1.00 |    0.03 | 0.0013 |      - |     112 B |        1.00 |
| ExpressionCompilation   | 23,766.17 ns | 2,377.252 ns | 130.305 ns | 1,059.41 |   22.20 | 0.0610 | 0.0305 |    5287 B |       47.21 |
| GenericTypeConstruction |     99.80 ns |    12.303 ns |   0.674 ns |     4.45 |    0.09 | 0.0020 |      - |     176 B |        1.57 |
| MethodInfoInvoke        |     66.55 ns |    10.997 ns |   0.603 ns |     2.97 |    0.06 | 0.0020 |      - |     176 B |        1.57 |
