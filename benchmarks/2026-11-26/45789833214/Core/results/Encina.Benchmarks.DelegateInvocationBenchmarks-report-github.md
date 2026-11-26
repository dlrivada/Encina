```

BenchmarkDotNet v0.15.8, Linux Ubuntu 26.04.1 LTS (Resolute Raccoon)
AMD EPYC 9V45 4.46GHz, 1 CPU, 4 logical and 2 physical cores
.NET SDK 10.0.404
  [Host]   : .NET 10.0.15 (10.0.15, 10.0.1526.53008), X64 RyuJIT x86-64-v4
  ShortRun : .NET 10.0.15 (10.0.15, 10.0.1526.53008), X64 RyuJIT x86-64-v4

Job=ShortRun  IterationCount=3  LaunchCount=1  
WarmupCount=3  

```
| Method                  | Mean         | Error        | StdDev     | Ratio  | RatioSD | Gen0   | Gen1   | Allocated | Alloc Ratio |
|------------------------ |-------------:|-------------:|-----------:|-------:|--------:|-------:|-------:|----------:|------------:|
| CompiledDelegate        |     18.66 ns |     2.507 ns |   0.137 ns |   0.83 |    0.01 | 0.0067 |      - |     112 B |        1.00 |
| DirectCall              |     22.56 ns |     0.521 ns |   0.029 ns |   1.00 |    0.00 | 0.0067 |      - |     112 B |        1.00 |
| ExpressionCompilation   | 15,261.11 ns | 3,312.587 ns | 181.574 ns | 676.47 |    7.01 | 0.3052 | 0.2747 |    5287 B |       47.21 |
| GenericTypeConstruction |     64.07 ns |    38.977 ns |   2.136 ns |   2.84 |    0.08 | 0.0105 |      - |     176 B |        1.57 |
| MethodInfoInvoke        |     41.37 ns |    10.186 ns |   0.558 ns |   1.83 |    0.02 | 0.0105 |      - |     176 B |        1.57 |
