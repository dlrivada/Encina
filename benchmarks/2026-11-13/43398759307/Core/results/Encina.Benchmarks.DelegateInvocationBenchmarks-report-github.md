```

BenchmarkDotNet v0.15.8, Linux Ubuntu 26.04.1 LTS (Resolute Raccoon)
AMD EPYC 9V74 3.67GHz, 1 CPU, 4 logical and 2 physical cores
.NET SDK 10.0.404
  [Host]   : .NET 10.0.15 (10.0.15, 10.0.1526.53008), X64 RyuJIT x86-64-v4
  ShortRun : .NET 10.0.15 (10.0.15, 10.0.1526.53008), X64 RyuJIT x86-64-v4

Job=ShortRun  IterationCount=3  LaunchCount=1  
WarmupCount=3  

```
| Method                  | Mean         | Error      | StdDev    | Ratio    | RatioSD | Gen0   | Gen1   | Allocated | Alloc Ratio |
|------------------------ |-------------:|-----------:|----------:|---------:|--------:|-------:|-------:|----------:|------------:|
| CompiledDelegate        |     27.10 ns |   2.165 ns |  0.119 ns |     1.26 |    0.01 | 0.0067 |      - |     112 B |        1.00 |
| DirectCall              |     21.43 ns |   0.549 ns |  0.030 ns |     1.00 |    0.00 | 0.0067 |      - |     112 B |        1.00 |
| ExpressionCompilation   | 53,555.84 ns | 739.250 ns | 40.521 ns | 2,499.53 |    3.45 | 0.3052 | 0.2441 |    5287 B |       47.21 |
| GenericTypeConstruction |    103.41 ns |  13.940 ns |  0.764 ns |     4.83 |    0.03 | 0.0105 |      - |     176 B |        1.57 |
| MethodInfoInvoke        |     66.20 ns |  14.897 ns |  0.817 ns |     3.09 |    0.03 | 0.0105 |      - |     176 B |        1.57 |
