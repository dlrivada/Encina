```

BenchmarkDotNet v0.15.8, Linux Ubuntu 26.04.1 LTS (Resolute Raccoon)
AMD EPYC 7763 2.82GHz, 1 CPU, 4 logical and 2 physical cores
.NET SDK 10.0.404
  [Host]   : .NET 10.0.15 (10.0.15, 10.0.1526.53008), X64 RyuJIT x86-64-v3
  ShortRun : .NET 10.0.15 (10.0.15, 10.0.1526.53008), X64 RyuJIT x86-64-v3

Job=ShortRun  IterationCount=3  LaunchCount=1  
WarmupCount=3  

```
| Method                  | Mean         | Error        | StdDev     | Ratio    | RatioSD | Gen0   | Gen1   | Allocated | Alloc Ratio |
|------------------------ |-------------:|-------------:|-----------:|---------:|--------:|-------:|-------:|----------:|------------:|
| CompiledDelegate        |     36.24 ns |    12.306 ns |   0.675 ns |     1.27 |    0.02 | 0.0067 |      - |     112 B |        1.00 |
| DirectCall              |     28.58 ns |     3.802 ns |   0.208 ns |     1.00 |    0.01 | 0.0067 |      - |     112 B |        1.00 |
| ExpressionCompilation   | 89,875.95 ns | 7,117.567 ns | 390.138 ns | 3,144.99 |   23.18 | 0.2441 | 0.1221 |    5275 B |       47.10 |
| GenericTypeConstruction |    129.55 ns |    27.513 ns |   1.508 ns |     4.53 |    0.05 | 0.0105 |      - |     176 B |        1.57 |
| MethodInfoInvoke        |     80.13 ns |     5.640 ns |   0.309 ns |     2.80 |    0.02 | 0.0105 |      - |     176 B |        1.57 |
