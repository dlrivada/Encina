```

BenchmarkDotNet v0.15.8, Linux Ubuntu 24.04.5 LTS (Noble Numbat)
Intel Xeon 6973P-C 2.60GHz, 1 CPU, 4 logical and 2 physical cores
.NET SDK 10.0.401
  [Host]   : .NET 10.0.12 (10.0.12, 10.0.1226.42308), X64 RyuJIT x86-64-v4
  ShortRun : .NET 10.0.12 (10.0.12, 10.0.1226.42308), X64 RyuJIT x86-64-v4

Job=ShortRun  IterationCount=3  LaunchCount=1  
WarmupCount=3  

```
| Method                  | Mean         | Error       | StdDev     | Ratio    | RatioSD | Gen0   | Gen1   | Allocated | Alloc Ratio |
|------------------------ |-------------:|------------:|-----------:|---------:|--------:|-------:|-------:|----------:|------------:|
| CompiledDelegate        |     28.93 ns |    14.57 ns |   0.798 ns |     1.28 |    0.05 | 0.0013 |      - |     112 B |        1.00 |
| DirectCall              |     22.65 ns |    16.44 ns |   0.901 ns |     1.00 |    0.05 | 0.0013 |      - |     112 B |        1.00 |
| ExpressionCompilation   | 25,038.01 ns | 3,759.82 ns | 206.088 ns | 1,106.67 |   39.22 | 0.0610 | 0.0305 |    5287 B |       47.21 |
| GenericTypeConstruction |     92.74 ns |    29.81 ns |   1.634 ns |     4.10 |    0.16 | 0.0020 |      - |     176 B |        1.57 |
| MethodInfoInvoke        |     69.56 ns |    33.34 ns |   1.827 ns |     3.07 |    0.13 | 0.0020 |      - |     176 B |        1.57 |
