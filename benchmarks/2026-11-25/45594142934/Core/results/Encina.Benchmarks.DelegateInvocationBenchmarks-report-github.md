```

BenchmarkDotNet v0.15.8, Linux Ubuntu 26.04.1 LTS (Resolute Raccoon)
AMD EPYC 9V74 2.81GHz, 1 CPU, 4 logical and 2 physical cores
.NET SDK 10.0.404
  [Host]   : .NET 10.0.15 (10.0.15, 10.0.1526.53008), X64 RyuJIT x86-64-v3
  ShortRun : .NET 10.0.15 (10.0.15, 10.0.1526.53008), X64 RyuJIT x86-64-v3

Job=ShortRun  IterationCount=3  LaunchCount=1  
WarmupCount=3  

```
| Method                  | Mean         | Error         | StdDev     | Ratio    | RatioSD | Gen0   | Gen1   | Allocated | Alloc Ratio |
|------------------------ |-------------:|--------------:|-----------:|---------:|--------:|-------:|-------:|----------:|------------:|
| CompiledDelegate        |     35.15 ns |      1.752 ns |   0.096 ns |     1.27 |    0.00 | 0.0067 |      - |     112 B |        1.00 |
| DirectCall              |     27.58 ns |      0.562 ns |   0.031 ns |     1.00 |    0.00 | 0.0067 |      - |     112 B |        1.00 |
| ExpressionCompilation   | 65,898.80 ns | 13,869.413 ns | 760.229 ns | 2,389.73 |   23.99 | 0.2441 | 0.1221 |    5275 B |       47.10 |
| GenericTypeConstruction |    130.41 ns |     29.424 ns |   1.613 ns |     4.73 |    0.05 | 0.0105 |      - |     176 B |        1.57 |
| MethodInfoInvoke        |     84.95 ns |      7.669 ns |   0.420 ns |     3.08 |    0.01 | 0.0105 |      - |     176 B |        1.57 |
