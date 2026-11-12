```

BenchmarkDotNet v0.15.8, Linux Ubuntu 26.04.1 LTS (Resolute Raccoon)
AMD EPYC 9V45 4.43GHz, 1 CPU, 4 logical and 2 physical cores
.NET SDK 10.0.404
  [Host]   : .NET 10.0.15 (10.0.15, 10.0.1526.53008), X64 RyuJIT x86-64-v4
  ShortRun : .NET 10.0.15 (10.0.15, 10.0.1526.53008), X64 RyuJIT x86-64-v4

Job=ShortRun  IterationCount=3  LaunchCount=1  
WarmupCount=3  

```
| Method                  | Mean         | Error        | StdDev     | Ratio    | RatioSD | Gen0   | Gen1   | Allocated | Alloc Ratio |
|------------------------ |-------------:|-------------:|-----------:|---------:|--------:|-------:|-------:|----------:|------------:|
| CompiledDelegate        |     20.10 ns |     4.145 ns |   0.227 ns |     1.28 |    0.02 | 0.0067 |      - |     112 B |        1.00 |
| DirectCall              |     15.71 ns |     2.498 ns |   0.137 ns |     1.00 |    0.01 | 0.0067 |      - |     112 B |        1.00 |
| ExpressionCompilation   | 17,130.39 ns | 7,123.728 ns | 390.476 ns | 1,090.77 |   23.06 | 0.3052 | 0.2747 |    5287 B |       47.21 |
| GenericTypeConstruction |     69.95 ns |    54.707 ns |   2.999 ns |     4.45 |    0.17 | 0.0105 |      - |     176 B |        1.57 |
| MethodInfoInvoke        |     44.75 ns |     1.984 ns |   0.109 ns |     2.85 |    0.02 | 0.0105 |      - |     176 B |        1.57 |
